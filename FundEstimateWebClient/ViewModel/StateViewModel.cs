namespace ViewModel
{
    public class StateViewModel<T>
    {
        public int Code { set; get; }
        public string Msg { set; get; }
        public T Response { set; get; }
        public int Count { set; get; }
    }
}