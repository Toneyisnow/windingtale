namespace WindingTale.Core.Common
{
    /// <summary>
    /// A simple callback signature that no return and no parameter.
    /// </summary>
    public delegate void DoCallback();

    /// <summary>
    /// Callback for one index number returned
    /// </summary>
    /// <param name="index"></param>
    public delegate void SelectionCallback(int index);

}