namespace Spice.Utilities {
    public abstract class  QuereStringParameter {
        private const int MaxSize = 50;
        public int PageNum { get; set; } = 1;
        private int _pageSize = 3;

        public int PageSize {
            get => _pageSize;
            set => _pageSize = value > MaxSize ? MaxSize : value;
        }

        public string OrderBy { get; set; }
    }
}