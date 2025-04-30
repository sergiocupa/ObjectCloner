using ObjectCloner;
using System.Diagnostics;
using System.Globalization;
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
            pai.Children.Add(new()
            {
                ID = 7778,
                Name = "GHI...",
                Parent = new ObjetoTestePai() { ID = 89565, Name = "ZZZ...", Options = new[] { new ObjetoTestePai() { ID = 766564, Name = "RRR...", Time = t2 } } }
            });

            pai.Options[0].Children.Add(new ObjetoTestePai() { ID = 4456, Name = "SSSSSS..." });

            var filho = new ObjetoTesteFilho() { ID = 87655, Name = "TTTTT...", Dependencies = new List<ObjetoTestePai>() };

            pai.Elements.Add(filho);

            filho.Dependencies.Add(new ObjetoTestePai() { ID = 3535529, Name = "WRJQDRI...", Time = t3 });


            pai.Children[1].Parent = pai;
            int SAMPLES = 1000000;

            var sww = Stopwatch.StartNew();

            var instances1 = new Dictionary<int, ObjetoTestePai>();
            var cp1 = new ObjetoTestePai2(pai, instances1);

            sww.Stop();
            var t10 = sww.Elapsed.TotalMicroseconds;
            sww.Restart();

            decimal sum = 0;
            int ix = 0;
            while (ix < SAMPLES)
            {
                sww.Restart();

                var instances = new Dictionary<int, ObjetoTestePai>();

                var cp2 = new ObjetoTestePai2(pai, instances);

                sww.Stop();
                sum += (decimal)sww.Elapsed.TotalMicroseconds;
                ix++;
            }
            var avg1 = sum / SAMPLES;



            sww.Stop();
            var t11 = sww.Elapsed.TotalMicroseconds;
            sww.Restart();


            var copy = Cloner.Clone(pai);

            ix = 0;
            while(ix < 10)
            {
                var copy1 = Cloner.Clone(pai);
                ix++;
            }

            var sw = Stopwatch.StartNew();
            sum = 0.0m;
            ix = 0;
            while (ix < SAMPLES)
            {
                sw.Restart();

                var copy1 = Cloner.Clone(pai);

                sw.Stop();
                sum += (decimal)sw.Elapsed.TotalMicroseconds;
                ix++;
            }
            var avg = sum / SAMPLES;

            UnitTest.Assert(() => copy != null);
            UnitTest.Assert(() => copy.ID == 5555 && copy.Name == "ABC..." && copy.Time == now);
            UnitTest.Assert(() => copy.Parent != null && copy.Parent.ID == 88 && copy.Parent.Name == "KLN");
            UnitTest.Assert(() => copy.Options != null && copy.Options.Length == 1);
            UnitTest.Assert(() => copy.Options[0].ID == 222 && copy.Options[0].Name == "DEF...");
            UnitTest.Assert(() => copy.Options[0].Children != null && copy.Options[0].Children.Count == 1);
            UnitTest.Assert(() => copy.Options[0].Children[0].ID == 4456 && copy.Options[0].Children[0].Name == "SSSSSS...");
            UnitTest.Assert(() => copy.Children != null && copy.Children.Count == 2);
            UnitTest.Assert(() => copy.Children[0].ID == 7777 && copy.Children[0].Name == "GHI...");
            UnitTest.Assert(() => copy.Children[0].Parent != null && copy.Children[0].Parent.ID == 987 && copy.Children[0].Parent.Name == "ZZZ...");
            UnitTest.Assert(() => copy.Children[0].Parent.Options != null && copy.Children[0].Parent.Options.Length == 1);
            UnitTest.Assert(() => copy.Children[0].Parent.Options[0].ID == 654 && copy.Children[0].Parent.Options[0].Name == "RRR..." && copy.Children[0].Parent.Options[0].Time == now);
            UnitTest.Assert(() => copy.Elements != null && copy.Elements.Count == 1);
            UnitTest.Assert(() => copy.Elements[0].ID == 87655 && copy.Elements[0].Name == "TTTTT...");
            UnitTest.Assert(() => copy.Elements[0].Dependencies != null && copy.Elements[0].Dependencies.Count == 1);
            UnitTest.Assert(() => copy.Elements[0].Dependencies[0].ID == 3535529 && copy.Elements[0].Dependencies[0].Name == "WRJQDRI..." && copy.Elements[0].Dependencies[0].Time == now);

            UnitTest.Assert(() => copy.Children.Count > 1);
            UnitTest.Assert(() => copy.Children[1].ID == 7778 && copy.Children[1].Name == "GHI...");
            UnitTest.Assert(() => copy.Children[1].Parent != null && copy.Children[1].Parent.ID == 5555);

            Console.WriteLine("Tempo copia do objeto (Construtor/Clonado) (us): " + avg1.ToString("0.000", CultureInfo.InvariantCulture) + " " +  avg.ToString("0.000", CultureInfo.InvariantCulture));
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


        public ObjetoTesteFilho(ObjetoTesteFilho a)
        {
            if(a != null)
            {
                ID = a.ID;
                Name = a.Name;

                if(a.Dependencies != null)
                {
                    Dependencies = new List<ObjetoTestePai>();

                    foreach(var b in  a.Dependencies)
                    {
                        Dependencies.Add(b);
                    }
                }
            }
        }

        public ObjetoTesteFilho()
        {

        }

    }


    public class ObjetoTestePai2 : ObjetoTestePai
    {
        public ObjetoTestePai2(ObjetoTestePai obj, Dictionary<int, ObjetoTestePai> instances)
        {
            if (obj != null)
            {
                ID     = obj.ID;
                Name   = obj.Name;
                Time   = obj.Time;

                if (obj.Parent != null)
                {
                    var h1 = obj.Parent.GetHashCode();
                    if (instances.TryGetValue(h1, out var v1))
                    {
                        Parent = v1;
                    }
                    else
                    {
                        instances.Add(h1, obj.Parent);
                        Parent = new ObjetoTestePai2(obj.Parent, instances);
                    }
                }
                //Parent = obj.Parent != null ? new ObjetoTestePai2(obj.Parent, instances) : null; 

                if (obj.Children != null)
                {
                    Children = new List<ObjetoTestePai>();
                    foreach (var a in obj.Children)
                    {
                        Children.Add(a);
                    }
                }

                if (obj.Elements != null)
                {
                    Elements = new List<ObjetoTesteFilho>();
                    foreach (var a in obj.Elements)
                    {
                        Elements.Add(new ObjetoTesteFilho(a));
                    }
                }

                if (obj.Options != null)
                {
                    Options = new ObjetoTestePai[obj.Options.Length];

                    int ix = 0;
                    while (ix < obj.Options.Length)
                    {
                        Options[ix] = new ObjetoTestePai2(obj.Options[ix], instances);
                        ix++;
                    }
                }
            }
        }
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
