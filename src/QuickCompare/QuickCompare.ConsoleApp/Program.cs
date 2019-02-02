using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using NDesk.Options;

namespace DacPacMan.ConsoleApp
{
    class Program
    {
        const string ProgName = "qc";
        public class ConnectionModel
        {
            public string DacpacFilename { get; set; }
            public string ConnectionString { get; set; }
            public string DatabaseName { get; set; }
            public string DatabaseServer { get; set; }
        }

        public class CompareeFactory
        {
            public IComparee GetComparee(ConnectionModel connectionModel, List<string> types)
            {
                if (!string.IsNullOrEmpty(connectionModel.DacpacFilename))
                {
                    return
                        new DacComparee(connectionModel.DacpacFilename,
                            types.Contains("Table") ? new DacTableProvider() : null,
                            types.Contains("View") ? new DacViewProvider() : null,
                            types.Contains("Procedure") ? new DacProcedureProvider() : null);
                }
                else
                {
                    var connectionString = !string.IsNullOrEmpty(connectionModel.ConnectionString) ? connectionModel.ConnectionString :
                        $"Data Source={connectionModel.DatabaseServer};Initial Catalog={connectionModel.DatabaseName};Persist Security Info=True;Trusted_Connection=True;";
                    return 
                        new DbComparee(connectionString,
                            types.Contains("Table") ? new DbTableProvider() : null,
                            types.Contains("View") ? new DbViewProvider() : null,
                            types.Contains("Procedure") ? new DbProcedureProvider() : null);
                }
            }
        }

        static void Main(string[] args)
        {
            var showHelp = false;
            var outputfile = @"C:\temp\output.sql";
            var actions = new List<string>();
            var types = new List<string>();
            var target = new ConnectionModel();
            var source = new ConnectionModel();

            var p = new OptionSet () {
                { "a|Action=", "Specifies a action (create, update, drop).",
                    v => actions.Add(v) },
                { "t|Type=", "Specifies a valid SQL Server object type to include in processing.",
                    v => types.Add(v) },
                { "o|OutputFile=", "Specifies a output file.",
                    v => outputfile=v },
                { "tf|TargetDacpacFilename=", "Specifies a valid dacpac file. If this parameter is specified, the target will be a dacpac.",
                    v => target.DacpacFilename=v },
                { "tcs|TargetConnectionString=", "Specifies a valid SQL Server connection string to the target database. If this parameter is specified, it shall be used exclusively of all other target parameters.",
                    v => target.ConnectionString=v },
                { "tdn|TargetDatabaseName=", "The target database name.",
                    v => target.DatabaseName=v },
                { "tds|TargetDatabaseServer=", "the target database server.",
                    v => target.DatabaseServer=v },
                { "sf|SourceDacpacFilename=", "Specifies a valid dacpac file. If this parameter is specified, the source will be a dacpac.",
                    v => source.DacpacFilename=v },
                { "scs|SourceConnectionString=", "Specifies a valid SQL Server connection string to the target database. If this parameter is specified, it shall be used exclusively of all other target parameters.",
                    v => source.ConnectionString=v },
                { "sdn|SourceDatabaseName=", "The target database name.",
                    v => source.DatabaseName=v },
                { "sds|SourceDatabaseServer=", "the target database server.",
                    v => source.DatabaseServer=v },
                //{ "v", "increase debug message verbosity",
                    //v => { if (v != null) ++verbosity; } },
                { "h|help",  "show this message and exit", 
                    v => showHelp = v != null },
            };

            try {
                p.Parse (args);
            }
            catch (OptionException e) {
                Console.Write ($"{ProgName}: ");
                Console.WriteLine (e.Message);
                Console.WriteLine ($"Try '{ProgName} --help' for more information.");
                return;
            }

            if (showHelp) {
                ShowHelp (p);
                return;
            }

            //var comparer = new DacpacToDbComparer("SQLPWV02", "Collections_Ods");
            //comparer.Compare();

            var compareeFactory = new CompareeFactory();
            var comparer = new Comparer(outputfile, 
                compareeFactory.GetComparee(source, types),
                compareeFactory.GetComparee(target, types));
            comparer.Compare();

            Console.WriteLine("Complete.");
            Console.ReadLine();
        }

        static void ShowHelp (OptionSet p)
        {
            Console.WriteLine ($"Usage: {ProgName} [OPTIONS]+");
            Console.WriteLine ("Compare dacpac(s) and/or database(s) and create sql script.");
            Console.WriteLine ();
            Console.WriteLine ("Options:");
            p.WriteOptionDescriptions (Console.Out);
        }
    }

    public class DacpacToDbComparer : Comparer
    {
        public DacpacToDbComparer(
            string dbServer,
            string dbName) 
            : base($@"C:\temp\{dbServer}.{dbName}.Drops.sql", 
                new DacComparee($@"E:\TFS\Collections\Collections.Databases\Development\Collections.Databases\{dbName}\bin\Output\{dbName}.dacpac",
                    new DacTableProvider(),
                    new DacViewProvider(),
                    new DacProcedureProvider()),
                new DbComparee($"Data Source={dbServer};Initial Catalog={dbName};Persist Security Info=True;Trusted_Connection=True;",
                    new DbTableProvider(),
                    new DbViewProvider(),
                    new DbProcedureProvider()))
        {
        }
    }

    public interface IObjectProvider
    {
        string Name { get; }
        List<string> Objects { get; }
        void ReadObjects();
    }

    public interface ITableProvider : IObjectProvider{}
    public interface IViewProvider : IObjectProvider{}
    public interface IProcedureProvider : IObjectProvider{}

    public abstract class ObjectProvider : IObjectProvider
    {
        public abstract string Name { get; }
        public List<string> Objects { get; } = new List<string>();
        public abstract void ReadObjects();
    }

    public abstract class DacObjectProvider : ObjectProvider
    {
        public TSqlModel Model { get; set; }
        public abstract ModelTypeClass TypeClass { get; }

        public override string Name => TypeClass.Name;

        public override void ReadObjects()
        {
            Objects.Clear();

            var objects = Model.GetObjects(DacQueryScopes.UserDefined, TypeClass);

            foreach (var obj in objects)
            {
                Objects.Add(obj.Name.ToString());
            }
        }
    } 

    public interface IDacTableProvider : ITableProvider{}
    public interface IDacViewProvider : IViewProvider{}
    public interface IDacProcedureProvider : IProcedureProvider{}

    public class DacTableProvider : DacObjectProvider, IDacTableProvider
    {
        public override ModelTypeClass TypeClass => Table.TypeClass;
    }

    public class DacViewProvider : DacObjectProvider, IDacViewProvider
    {
        public override ModelTypeClass TypeClass => View.TypeClass;
    }

    public class DacProcedureProvider : DacObjectProvider, IDacProcedureProvider
    {
        public override ModelTypeClass TypeClass => Procedure.TypeClass;
    }

    public interface IComparee
    {
        List<IObjectProvider> ObjectProviders { get; }

        ITableProvider TableProvider { get; }
        IViewProvider ViewProvider { get; }
        IProcedureProvider ProcedureProvider { get; }

        void ReadMetadata();
    }

    public abstract class Comparee : IComparee
    {
        public List<IObjectProvider> ObjectProviders { get; } = new List<IObjectProvider>();

        public ITableProvider TableProvider { get; protected set; }
        public IViewProvider ViewProvider { get; protected set; }
        public IProcedureProvider ProcedureProvider { get; protected set; }

        public Comparee(
            ITableProvider tableProvider,
            IViewProvider viewProvider,
            IProcedureProvider procedureProvider)
        {
            TableProvider = tableProvider;
            ViewProvider = viewProvider;
            ProcedureProvider = procedureProvider;

            AddObjectProvider(tableProvider);
            AddObjectProvider(viewProvider);
            AddObjectProvider(procedureProvider);
        }

        protected void AddObjectProvider(IObjectProvider provider)
        {
            if(provider != null)
                ObjectProviders.Add(provider);
        }

        public abstract void ReadMetadata();
    }

    public class DacComparee : Comparee
    {
        public string DacpacFilename { get; }

        public IDacTableProvider DacTableProvider { get; }
        public IDacViewProvider DacViewProvider { get; }
        public IDacProcedureProvider DacProcedureProvider { get; }

        public DacComparee(string dacpacFilename,
            IDacTableProvider tableProvider,
            IDacViewProvider viewProvider,
            IDacProcedureProvider procedureProvider)
        : base(tableProvider, viewProvider, procedureProvider)
        {
            DacpacFilename = dacpacFilename;

            DacTableProvider = tableProvider;
            DacViewProvider = viewProvider;
            DacProcedureProvider = procedureProvider;
        }

        public override void ReadMetadata()
        {
            using (var model = new TSqlModel(DacpacFilename))
            {
                ObjectProviders.ForEach(o =>
                {
                    if (o is DacObjectProvider provider)
                    {
                        provider.Model = model;
                        provider.ReadObjects();
                    }
                });
            }
        }
    }

    public abstract class DbObjectProvider : ObjectProvider
    {
        public string ConnectionString { get; set; }
        public virtual string Filter => "";
        //
        public override void ReadObjects()
        {
            Objects.Clear();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using(var cmd = new SqlCommand($"SELECT t.name, s.name schemaname FROM sys.{Name}s t join sys.schemas s ON t.schema_id = s.schema_id {Filter}", connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Objects.Add($"[{reader["schemaname"]}].[{reader["name"]}]");
                    }
                }

                connection.Close();
            }
        }
    } 

    public interface IDbTableProvider : ITableProvider{}
    public interface IDbViewProvider : IViewProvider{}
    public interface IDbProcedureProvider : IProcedureProvider{}

    public class DbTableProvider : DbObjectProvider, IDbTableProvider
    {
        public override string Name => "Table";
        public override string Filter => "where t.name not like '__%'";
    }

    public class DbViewProvider : DbObjectProvider, IDbViewProvider
    {
        public override string Name => "View";
    }

    public class DbProcedureProvider : DbObjectProvider, IDbProcedureProvider
    {
        public override string Name => "Procedure";
        public override string Filter => "where t.name not like 'sp_%'";
    }

    public class DbComparee : Comparee
    {
        public string ConnectionString { get; }

        public IDbTableProvider DbTableProvider { get; }
        public IDbViewProvider DbViewProvider { get; }
        public IDbProcedureProvider DbProcedureProvider { get; }

        public DbComparee(string connectionString,
            IDbTableProvider tableProvider,
            IDbViewProvider viewProvider,
            IDbProcedureProvider procedureProvider)
            : base(tableProvider, viewProvider, procedureProvider)
        {
            ConnectionString = connectionString;

            DbTableProvider = tableProvider;
            DbViewProvider = viewProvider;
            DbProcedureProvider = procedureProvider;
        }

        public override void ReadMetadata()
        {
            ObjectProviders.ForEach(o =>
            {
                if (o is DbObjectProvider provider)
                {
                    provider.ConnectionString = ConnectionString;
                    provider.ReadObjects();
                }
            });
        }
    }

    public interface IComparer
    {
        void Compare();
    }

    public class Comparer : IComparer
    {
        public string OutputFilename { get; }
        public IComparee Source { get; }
        public IComparee Target { get; }

        public Comparer(string outputFilename, IComparee source, IComparee target)
        {
            OutputFilename = outputFilename;
            Source = source;
            Target = target;
        }

        public void Compare()
        {
            Source.ReadMetadata();
            Target.ReadMetadata();

            using (TextWriter writter = new StreamWriter(OutputFilename))
            {
                Target.ObjectProviders.ForEach(to =>
                {
                    writter.WriteLine($@"--{to.Name}");
                    var so = Source.ObjectProviders.FirstOrDefault(o => o.Name == to.Name);
                    if (so != null)
                    {
                        var objs = to.Objects.Except(so.Objects).ToList();
                        objs.ForEach(o =>
                        {
                            writter.WriteLine($@"IF OBJECT_ID('{o}') IS NOT NULL DROP {to.Name} {o};");
                        });
                    }
                });
            }
        }
    }
}
//var DbServer = "SQLDWV01";
//var DbName = "Collections_Ods";
//var odsConnectiongString = $"Data Source={DbServer};Initial Catalog={DbName};Persist Security Info=True;Trusted_Connection=True;";
//var odsDacpac = @"E:\TFS\Collections\Collections.Databases\Development\Collections.Databases\Collections_Ods\bin\Output\Collections_Ods.dacpac";
//var output = $@"C:\temp\{DbServer}.{DbName}.Drops.sql";
//var comparer = new Comparer(output,
//    new DacComparee(odsDacpac,
//        new DacTableProvider(),
//        new DacViewProvider(),
//        new DacProcedureProvider()), 
//    new DbComparee(odsConnectiongString,
//        new DbTableProvider(),
//        new DbViewProvider(),
//        new DbProcedureProvider()));