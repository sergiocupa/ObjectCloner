namespace ObjectCloner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var gg = new ObjetoTestePai()
            {
                ID = 5555,
                Parent = new ObjetoTestePai() { ID = 6666 },
                //Atributo = new ObjetoTesteFilho() { ID = 123, Name = "gggg" },
                //ChildArray = new[]
                //{
                //    new ObjetoTesteFilho() { ID = 456, Name = "ABC..." }
                //},
                //Arrays = new[]
                //{
                //    new ObjetoTestePai() { ID = 789, Parent = new ObjetoTestePai(){ ID = 999 } }
                //},
                Children = new List<ObjetoTestePai>()
            };
            gg.Children.Add(new ObjetoTestePai() { ID = 7777 });

            // Como pode ter instancias circular, testar instancia ativa.
            // Para cada propriedade, registrar os niveis de reentrada.
            // Como resolver referencia circular, por lista???

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
        //public ObjetoTesteFilho Atributo { get; set; }
        public ObjetoTestePai Parent { get; set; }
        public List<ObjetoTestePai> Children { get; set; }
        //public ObjetoTesteFilho[] ChildArray { get; set; }
        //public ObjetoTestePai[] Arrays { get; set; }
    }
}
