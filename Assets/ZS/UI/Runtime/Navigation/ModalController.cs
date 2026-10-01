namespace ZS.UI.Navigation
{
    /// <summary>
    /// Modal that returns a result to whoever opened it:
    /// <c>var ok = await navigator.ShowModal&lt;bool&gt;(confirmDefinition);</c>.
    /// Closing it in any other way (back, backdrop) returns <c>default</c>.
    /// </summary>
    public abstract class ModalController<TResult> : ViewController
    {
        protected void Close(TResult result)
        {
            Context.Navigator.CloseModal(this, result);
        }
    }
}
