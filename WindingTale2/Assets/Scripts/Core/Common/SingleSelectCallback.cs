namespace WindingTale.Core.Common
{
    ////public delegate void SelectionCallback(int index);

    /// <summary>
    /// A simple callback signature that returns a number as result.
    /// 
    /// </summary>
    public class SingleSelectCallback
    {
        public SingleSelectCallback(SelectionCallback callback)
        {
            this.callback = callback;
        }

        
        private SelectionCallback callback = null;

        public void Invoke(int index)
        {
            if (this.callback != null)
            {
                this.callback(index);
            }
        }
    }
}