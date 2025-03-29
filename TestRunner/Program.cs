using ObjectCloner;
using UnitTester;

namespace TestRunner
{
    internal class Program
    {

        static void BasicCloneTest()
        {
            var now = DateTime.Now;
            var t1 = new DateTime(now.Ticks);
            var t2 = new DateTime(now.Ticks);
            var t3 = new DateTime(now.Ticks);


            var pai = new ObjetoTestePai()
            {
                Parent = new ObjetoTestePai() { Name = "KLN", ID = 88 },
                Name = "ABC...",
                Children = new(),
                ID = 5555,
                Time = t1,
                Options = new ObjetoTestePai[] { new ObjetoTestePai() { ID = 222, Name = "DEF...", Children = new List<ObjetoTestePai>() } },
                Elements = new List<ObjetoTesteFilho>()
            };
            pai.Children.Add(new()
            {
                ID = 7777,
                Name = "GHI...",
                Parent = new ObjetoTestePai() { ID = 987, Name = "ZZZ...", Options = new[] { new ObjetoTestePai() { ID = 654, Name = "RRR...", Time = t2 } } }
            });

            pai.Options[0].Children.Add(new ObjetoTestePai() { ID = 4456, Name = "SSSSSS..." });

            var filho = new ObjetoTesteFilho() { ID = 87655, Name = "TTTTT...", Dependencies = new List<ObjetoTestePai>() };

            pai.Elements.Add(filho);

            filho.Dependencies.Add(new ObjetoTestePai() { ID = 3535529, Name = "WRJQDRI...", Time = t3 });

            var copy = Cloner.Clone(pai);

            UnitTest.Assert(() => copy != null);
            UnitTest.Assert(() => copy.ID == 5555 && copy.Name == "ABC..." && copy.Time == now);
            UnitTest.Assert(() => copy.Parent != null && copy.Parent.ID == 88 && copy.Parent.Name == "KLN");
            UnitTest.Assert(() => copy.Options != null && copy.Options.Length == 1);
            UnitTest.Assert(() => copy.Options[0].ID == 222 && copy.Options[0].Name == "DEF...");
            UnitTest.Assert(() => copy.Options[0].Children != null && copy.Options[0].Children.Count == 1);
            UnitTest.Assert(() => copy.Options[0].Children[0].ID == 4456 && copy.Options[0].Children[0].Name == "SSSSSS...");
            UnitTest.Assert(() => copy.Children != null && copy.Children.Count == 1);
            UnitTest.Assert(() => copy.Children[0].ID == 7777 && copy.Children[0].Name == "GHI...");
            UnitTest.Assert(() => copy.Children[0].Parent != null && copy.Children[0].Parent.ID == 987 && copy.Children[0].Parent.Name == "ZZZ...");
            UnitTest.Assert(() => copy.Children[0].Parent.Options != null && copy.Children[0].Parent.Options.Length == 1);
            UnitTest.Assert(() => copy.Children[0].Parent.Options[0].ID == 654 && copy.Children[0].Parent.Options[0].Name == "RRR..." && copy.Children[0].Parent.Options[0].Time == now);
            UnitTest.Assert(() => copy.Elements != null && copy.Elements.Count == 1);
            UnitTest.Assert(() => copy.Elements[0].ID == 87655 && copy.Elements[0].Name == "TTTTT...");
            UnitTest.Assert(() => copy.Elements[0].Dependencies != null && copy.Elements[0].Dependencies.Count == 1);
            UnitTest.Assert(() => copy.Elements[0].Dependencies[0].ID == 3535529 && copy.Elements[0].Dependencies[0].Name == "WRJQDRI..." && copy.Elements[0].Dependencies[0].Time == now);
        }


        static void Main(string[] args)
        {
            BasicCloneTest();
            Console.WriteLine("Success!!!!");
        }

    }


    public class ObjetoTesteFilho
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public List<ObjetoTestePai> Dependencies { get; set; }
    }

    public class ObjetoTestePai
    {
        public List<ObjetoTestePai> Children { get; set; }
        public ObjetoTestePai[] Options { get; set; }
        public ObjetoTestePai Parent { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime Time { get; set; }
        public List<ObjetoTesteFilho> Elements { get; set; }
    }



}
