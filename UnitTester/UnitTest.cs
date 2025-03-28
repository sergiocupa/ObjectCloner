using System.Linq.Expressions;
using System.Runtime.CompilerServices;


namespace UnitTester
{
    public class UnitTest
    {
        public static void Assert(Expression<Func<bool>> predicate, [CallerFilePath] string path = "", [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            string file = Path.GetFileName(path);

            var result = predicate.Compile().Invoke();

            var printer = new ExpressionPrinter();
            var exptx   = printer.Print(predicate.Body);

            var header =
            "TEST(B0E682708E5B4F49B23FA71BD6899D71) | " +
            "Time: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm.ss.fffff") + " | " +
            "File: " + file + " | " +
            "Method: " + caller + " | " +
            "Line: " + line + " | " +
            "Result: " + result + "\n" +
            "    " + exptx;

            if (result)
            {
                Console.WriteLine(header);
            }
            else
            {
                Console.Error.WriteLine(header);
                Environment.Exit((int)EnvironmentReturn.ERROR);
            }
        }
        
    }

    public enum EnvironmentReturn : int
    {
        SUCCESS = 0,
        ERROR = 1
    }
}
