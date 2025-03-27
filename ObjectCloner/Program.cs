namespace ObjectCloner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var gg = new ObjetoTestePai()
            {
                Children = new(),
                ID = 5555
            };
            gg.Children.Add(new () { ID = 7777 });


            var copy = Cloner.Clone(gg);

            string jj = "";
        }
    }



    public class ObjetoTesteFilho
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    public class ObjetoTestePai
    {
        public int ID { get; set; }
        public List<ObjetoTestePai> Children { get; set; }
    }

}
