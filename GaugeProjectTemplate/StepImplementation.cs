using System;
using System.Linq;
using Gauge.CSharp.Lib;
using Gauge.CSharp.Lib.Attribute;

namespace _GaugeProjectTemplate
{
	public class StepImplementation
	{
        [Step("A context step which gets executed before every scenario")]
        public void Context()
        {
            Console.WriteLine("This is a sample context");
        }

        [Step("Say <what> to <who>")]
        public void SaySomething(string what, string who)
        {
            Console.WriteLine("{0}, {1}!", what, who);
        }

        [Step("Step that takes a table <table>")]
        public void ReadTable(Table table)
        {
            Func<string, string, string> aggregateFunc = (a, b) => string.Format("{0}|{1}", a, b);
            var columnNames = table.GetColumnNames();

            // print column headers
            Console.WriteLine(columnNames.Aggregate(aggregateFunc));

            //print row cells
            foreach (var row in table.GetTableRows())
            {
                // Get all row cells, and print them in a line
                Console.WriteLine(columnNames.Select(s => row.GetCell(s)).Aggregate(aggregateFunc));
            }
        }
    }
}
