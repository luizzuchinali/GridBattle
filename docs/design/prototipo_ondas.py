#!/usr/bin/env python3
"""
Protótipo do gerador de ondas (proposta, ver "proposta_conteudo_classes_skills_estados_talentos.md", seção 9).

Objetivo: validar a ideia ANTES de escrever C#. Não é código de produção e não mede dificuldade real.
O que ele mostra: determinismo (mesma seed + onda = mesma onda), uso do orçamento de ameaça,
variedade de composições e os efeitos dos limites (50% por tipo, elites ocupando 3 vagas).

Uso:  python3 prototipo_ondas.py            (imprime exemplos e estatísticas)
"""
import collections
import math
import statistics

# ---------------------------------------------------------------- PRNG próprio (SplitMix64)
# Em C#, usar um gerador com algoritmo fixo (SplitMix64, PCG ou xoshiro). Evitar UnityEngine.Random
# e System.Random na lógica: o algoritmo não é garantido entre versões e plataformas.
M64 = (1 << 64) - 1


def splitmix(x):
    x = (x + 0x9E3779B97F4A7C15) & M64
    z = x
    z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & M64
    z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & M64
    return z ^ (z >> 31)


class Rng:
    """Fluxo de números aleatórios inicializado por hash(partes...), por exemplo (seed, fluxo, onda)."""

    def __init__(self, *parts):
        h = 0
        for p in parts:
            h = splitmix(h ^ (p & M64))
        self.s = h

    def next(self):
        self.s = splitmix(self.s)
        return self.s

    def rand(self):
        return (self.next() >> 11) / float(1 << 53)

    def pick(self, items, weights):
        total = sum(weights)
        r = self.rand() * total
        acc = 0
        for item, w in zip(items, weights):
            acc += w
            if r < acc:
                return item
        return items[-1]


# ---------------------------------------------------------------- Pool de inimigos
def ameaca(vida, dano, mov, alcance, fator_papel=1.0):
    """dano * vida / 50 * (1 + 0,25 * (mov - 1)) * (1 + 0,5 * (alcance - 1)) * fator de papel"""
    return dano * vida / 50 * (1 + 0.25 * (mov - 1)) * (1 + 0.5 * (alcance - 1)) * fator_papel


# nome: (papel, vida, dano, mov, alcance, fator de papel, onda mínima, peso)
POOL = {
    "Goblin":    ("bruto",     30,  5, 1, 1, 1.0,  1, 10),
    "Rat":       ("enxame",    20,  5, 2, 1, 1.0,  1, 10),
    "Slime":     ("bruto",     40,  5, 1, 1, 1.0,  1,  8),
    "EyeBat":    ("enxame",    25,  5, 2, 1, 1.0,  2,  8),
    "FireSkull": ("bruto",     25,  7, 1, 1, 1.0,  3,  8),
    "Arqueiro":  ("distancia", 20,  6, 1, 2, 1.0,  4,  8),
    "Berserker": ("bruto",     35,  5, 1, 1, 1.0,  5,  6),
    "Aranha":    ("controle",  25,  4, 1, 2, 1.3,  6,  6),
    "Cultista":  ("suporte",   25,  3, 1, 1, 2.0,  7,  5),
    "Bomba":     ("especial",  10, 12, 2, 1, 1.0,  8,  4),
    "Bruxa":     ("controle",  22,  5, 1, 2, 1.3,  9,  4),
    "Golem":     ("bruto",     80,  8, 1, 1, 0.6, 12,  3),
    "Invocador": ("suporte",   30,  3, 1, 2, 2.0, 14,  3),
}
T = {k: ameaca(*v[1:6]) for k, v in POOL.items()}
ORIGINAIS = ["Goblin", "Rat", "Slime", "FireSkull", "EyeBat"]
T_MEDIA = sum(T[k] for k in ORIGINAIS) / len(ORIGINAIS)  # 3,23

LIMITE_POR_TIPO_PAPEL = {"controle": 2, "suporte": 2, "especial": 2, "distancia": 4}
MULT_ELITE = 3.0  # um elite custa 3x a ameaça média e ocupa 3 vagas


# ---------------------------------------------------------------- Parâmetros por onda
def contagem(onda):
    return min(12, 3 + onda // 2)


def fator_vida(onda):
    return 1 + 0.05 * (onda - 1)


def fator_dano(onda):
    return 1 + 0.04 * (onda - 1)


# ---------------------------------------------------------------- Gerador
def gerar(seed, onda):
    """Retorna (vagas, orçamento, ameaça usada, composição, elites). Função pura."""
    rng = Rng(seed, 0xA11CE, onda)  # fluxo "ondas"
    n = contagem(onda)
    escala = fator_vida(onda) * fator_dano(onda)
    orcamento = n * T_MEDIA * escala

    disponiveis = [k for k, v in POOL.items() if v[6] <= onda]
    n_tipos = min(len(disponiveis), 2 if onda < 4 else (3 if onda < 10 else 4))

    def peso(k):  # tipos que entraram nas últimas 2 ondas pesam x2
        return POOL[k][7] * (2.0 if POOL[k][6] >= onda - 2 else 1.0)

    # 1) tipos: o primeiro é sempre corpo a corpo
    corpo_a_corpo = [k for k in disponiveis if POOL[k][0] in ("bruto", "enxame")]
    tipos = [rng.pick(corpo_a_corpo, [peso(k) for k in corpo_a_corpo])]
    resto = [k for k in disponiveis if k != tipos[0]]
    while resto and len(tipos) < n_tipos:
        k = rng.pick(resto, [peso(k) for k in resto])
        tipos.append(k)
        resto.remove(k)

    # 2) elites (ondas de 6 em diante, exceto chefe)
    elites = 0
    if onda >= 6 and onda % 10 != 0:
        p = min(0.6, 0.06 * (onda - 5))
        max_elites = 1 + onda // 20
        elites = max_elites if rng.rand() < p else 0

    vagas = max(1, n - 3 * elites)
    teto_tipo = math.ceil(vagas * 0.5)  # nenhum tipo passa de 50% das vagas
    r = orcamento - elites * T_MEDIA * MULT_ELITE * escala
    comp = collections.Counter()
    t_min = min(T[k] for k in tipos) * escala

    # 3a) cada tipo escolhido entra uma vez, se couber
    for j, k in enumerate(tipos[:vagas]):
        faltam = vagas - sum(comp.values()) - 1
        if j == 0 or T[k] * escala <= r - faltam * t_min + 1e-9:
            comp[k] += 1
            r -= T[k] * escala

    # 3b) vagas restantes: sorteio ponderado respeitando limites e orçamento
    restantes = vagas - sum(comp.values())
    for i in range(restantes):
        faltam = restantes - i
        ok = [
            k for k in tipos
            if comp[k] < teto_tipo
            and comp[k] < LIMITE_POR_TIPO_PAPEL.get(POOL[k][0], 99)
            and T[k] * escala <= r - (faltam - 1) * t_min + 1e-9
        ]
        if not ok:
            ok = [k for k in tipos if comp[k] < teto_tipo] or tipos
        k = rng.pick(ok, [POOL[k][7] for k in ok])
        comp[k] += 1
        r -= T[k] * escala

    usado = sum(T[k] * escala * c for k, c in comp.items()) + elites * T_MEDIA * MULT_ELITE * escala
    return n, orcamento, usado, dict(comp), elites


if __name__ == "__main__":
    print("Ameaça por inimigo")
    for k, v in POOL.items():
        print(f"  {k:10s} {v[0]:9s} vida {v[1]:3d} dano {v[2]:2d} mov {v[3]} alc {v[4]} -> {T[k]:.2f}")
    print(f"ameaça média dos 5 originais: {T_MEDIA:.2f}\n")

    print("Exemplos (seed 12345)")
    for onda in (1, 3, 5, 8, 9, 15, 19, 29, 39):
        n, b, u, c, e = gerar(12345, onda)
        print(f"  onda {onda:2d}: vagas {n:2d}  orçamento {b:6.1f}  usado {u:6.1f} ({u / b * 100:4.0f}%)  "
              f"elites {e}  total {sum(c.values()) + e}  {c}")

    print("\nDeterminismo:", gerar(12345, 19) == gerar(12345, 19), "| seed diferente gera outra onda:",
          gerar(777, 10) != gerar(12345, 10))

    print("\nEstatística (3.000 sementes por onda)")
    for onda in (1, 5, 9, 19, 29, 39):
        usos, tipos_n, dist, fatias, els = [], [], collections.Counter(), [], []
        for s in range(3000):
            n, b, u, c, e = gerar(s, onda)
            usos.append(u / b)
            tipos_n.append(len(c))
            dist[tuple(sorted(c.items()))] += 1
            fatias.append(max(c.values()) / max(1, sum(c.values())))
            els.append(e)
        fora = sum(1 for x in usos if x > 1.15) / len(usos) * 100
        print(f"  onda {onda:2d}: uso médio {statistics.mean(usos) * 100:4.0f}% (mín {min(usos) * 100:.0f}%, "
              f"acima de 115%: {fora:.1f}%) | tipos {statistics.mean(tipos_n):.1f} | maior fatia "
              f"{statistics.mean(fatias) * 100:.0f}% | elites {statistics.mean(els):.2f} | composições distintas {len(dist)}")

    print("\nChefes (vida = 0,72 x vida total normal; dano = 2 x dano escalado)")
    vida_media = statistics.mean(POOL[k][1] for k in ORIGINAIS)
    for onda in (10, 20, 30, 40):
        total = contagem(onda) * vida_media * fator_vida(onda)
        print(f"  onda {onda}: vida total normal {total:.0f} -> chefe {0.72 * total:.0f} de vida, "
              f"dano {2 * 5 * fator_dano(onda):.1f}, escolta ~{round(0.4 * contagem(onda))} inimigos")
