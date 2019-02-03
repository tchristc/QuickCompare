# QuickCompare
Usage:

QC -a drop -t Table -t View -t Procedure -o "C:\temp\output2.txt" -sf "E:\TFS\Collections\Collections.Databases\Development\Collections.Databases\Collections_Ods\bin\Output\Collections_Ods.dacpac" -tcs "Data Source=SQLDWV01;Initial Catalog=Collections_Ods;Persist Security Info=True;Trusted_Connection=True;"

QC -help

Usage: qc [OPTIONS]+
Compare dacpac(s) and/or database(s) and create sql script.

Options:
  -a, --Action=VALUE         Specifies a action (create, update, drop).
  -t, --Type=VALUE           Specifies a valid SQL Server object type to 
                               include in processing.
  -o, --OutputFile=VALUE     Specifies a output file.
      --tf, --TargetDacpacFilename=VALUE
                             Specifies a valid dacpac file. If this parameter 
                               is specified, the target will be a dacpac.
      --tcs, --TargetConnectionString=VALUE
                             Specifies a valid SQL Server connection string 
                               to the target database. If this parameter is 
                               specified, it shall be used exclusively of all 
                               other target parameters.
      --tdn, --TargetDatabaseName=VALUE
                             The target database name.
      --tds, --TargetDatabaseServer=VALUE
                             the target database server.
      --sf, --SourceDacpacFilename=VALUE
                             Specifies a valid dacpac file. If this parameter 
                               is specified, the source will be a dacpac.
      --scs, --SourceConnectionString=VALUE
                             Specifies a valid SQL Server connection string 
                               to the target database. If this parameter is 
                               specified, it shall be used exclusively of all 
                               other target parameters.
      --sdn, --SourceDatabaseName=VALUE
                             The target database name.
      --sds, --SourceDatabaseServer=VALUE
                             the target database server.
  -h, --help                 show this message and exit
