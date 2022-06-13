using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace FundsEstimate
{
    public class ProcessFundsEstimate
    {
        /// <summary>
        /// Working dir is 'c:\WorkingFilesFromIsugf\FundEstimate\'
        /// </summary>
        /// <param name="workingDir"></param>
        [SqlProcedure]
        public static void SaveFundEstimateToDb(string workingDir)
        {
            try
            {
                List<FileInfo> fundEstimateStatusFiles = new DirectoryInfo(workingDir).GetFiles("MFERINF*.*").OrderBy(x => x.Name).ToList();
                for (int i = 0; i < fundEstimateStatusFiles.Count; i++)
                {
                    long lastInsertedId = SaveFileNameToDb(fundEstimateStatusFiles, i);
                    using (SqlConnection sqlConnection = new SqlConnection("context connection=true"))
                    {
                        SqlTransaction transaction;
                        sqlConnection.Open();
                        transaction = sqlConnection.BeginTransaction();
                        try
                        {
                            using (StreamReader sr = new StreamReader(fundEstimateStatusFiles[i].FullName, Encoding.GetEncoding(1251)))
                            {
                                string line;

                                while ((line = sr.ReadLine()) != null && line.Trim().Length != 0)
                                {
                                    if (line.StartsWith("S"))
                                    {
                                        string[] separated_words = line.Split((char)29);

                                        DataTable dtFundEstimate = AddFundEstimateColumns();

                                        AddRowFundEstimate(dtFundEstimate, lastInsertedId, separated_words);

                                        FundEstimateBulkCopy(sqlConnection, dtFundEstimate, transaction);
                                    }
                                }
                                UpdateLogTableAsProcessed(sqlConnection, lastInsertedId, transaction);
                                transaction.Commit();

                            }
                            string destinationFilename = workingDir + @"\archive\" + fundEstimateStatusFiles[i].Name;
                            MoveFundEstimateFileToArchive(fundEstimateStatusFiles[i].FullName, destinationFilename);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            SqlContext.Pipe.Send(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send(ex.Message);
            }
        }

        #region Methods        
        /// <summary>
        /// Insert file name with status to FundEstimateLog table.
        /// </summary>

        private static long SaveFileNameToDb(List<FileInfo> fundRequestStatusFiles, int i)
        {
            using (SqlConnection myConn = new SqlConnection("context connection=true"))
            {
                myConn.Open();
                SqlCommand myComm = new SqlCommand("INSERT INTO FundEstimateLog (FileName,Status) VALUES ('" + fundRequestStatusFiles[i].Name + "','Processing')  select [ID] from FundEstimateLog where @@ROWCOUNT > 0 and [ID] = scope_identity()", myConn);
                long lastInsertedId = (long)myComm.ExecuteScalar();

                return lastInsertedId;
            }
        }
        /// <summary>
        /// Add columns to dtFundEstimate table
        /// </summary>
        /// <returns></returns>
        private static DataTable AddFundEstimateColumns()
        {
            DataTable dtFundEstimate = new DataTable();
            dtFundEstimate.Columns.Add("TYPE", typeof(string));
            dtFundEstimate.Columns.Add("ID", typeof(long));
            dtFundEstimate.Columns.Add("ACTION", typeof(string));
            dtFundEstimate.Columns.Add("ACC", typeof(string));
            dtFundEstimate.Columns.Add("FINYEAR", typeof(int));
            dtFundEstimate.Columns.Add("SMETA_TYPE", typeof(string));
            dtFundEstimate.Columns.Add("EXPENSE", typeof(string));
            dtFundEstimate.Columns.Add("MONTH", typeof(int));
            dtFundEstimate.Columns.Add("SUMPAY", typeof(long));
            dtFundEstimate.Columns.Add("DOCDATE", typeof(DateTime));
            dtFundEstimate.Columns.Add("FundEstimateLogId", typeof(long));
            return dtFundEstimate;
        }
        /// <summary>
        /// Add rows to dtFundEstimate table
        /// </summary>
        private static void AddRowFundEstimate(DataTable dtFundEstimate, long lastInsertedLogId, string[] words)
        {
            var row = dtFundEstimate.NewRow();
            row["FundEstimateLogId"] = lastInsertedLogId;
            if (words[0] == "S")
            {
                row["TYPE"] = words[0];
                row["ID"] = Convert.ToInt64(words[1]);
                row["ACTION"] = words[2];
                row["ACC"] = words[3];
                row["FINYEAR"] = Convert.ToInt32(words[4]);
                row["SMETA_TYPE"] = words[5];
                row["EXPENSE"] = words[6];
                row["MONTH"] = Convert.ToInt32(words[7]);
                row["SUMPAY"] = words[8];
                row["DOCDATE"] = DateTime.ParseExact(words[9], "ddMMyyyy", System.Globalization.CultureInfo.InvariantCulture).ToString("dd-MM-yyyy");
                dtFundEstimate.Rows.Add(row);
            }
        }
        /// <summary>
        /// Bulk copy dtFundEstimate DataTable to FundEstimate table
        /// Change Status column of FundEstimateLog table as 'Processed'
        /// </summary>

        private static void FundEstimateBulkCopy(SqlConnection conn, DataTable dtFundEstimate, SqlTransaction sqlTransaction)
        {
            try
            {

                using (var copy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, sqlTransaction))
                {
                    foreach (DataColumn cl in dtFundEstimate.Columns)
                        copy.ColumnMappings.Add(cl.ColumnName, cl.ColumnName);
                    copy.BulkCopyTimeout = 0;
                    copy.BatchSize = 2500;
                    copy.DestinationTableName = "FundEstimate";
                    copy.WriteToServer(dtFundEstimate);
                }
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send(ex.Message);
            }
            dtFundEstimate.Clear();
            dtFundEstimate.Dispose();
        }
        // <summary>
        /// Update log table as processed.
        /// </summary>
        private static void UpdateLogTableAsProcessed(SqlConnection conn, long lastInsertedId, SqlTransaction sqlTransaction)
        {
            try
            {
                SqlCommand myCommStatus = new SqlCommand("UPDATE FundEstimateLog Set Status='Processed' WHERE ID=" + lastInsertedId + "", conn, sqlTransaction);
                myCommStatus.ExecuteNonQuery();
                myCommStatus.Dispose();
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send(ex.Message);
            }
        }

        /// <summary>
        /// Moves files from working direktory to archive folder
        /// </summary>
        private static void MoveFundEstimateFileToArchive(string sourceFileName, string destinationFileName)
        {
            try
            {
                if (File.Exists(destinationFileName))
                    File.Delete(destinationFileName);

                File.Copy(sourceFileName, destinationFileName);
                File.Delete(sourceFileName);

                SqlContext.Pipe.Send(sourceFileName + " File deleted");
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send(ex.Message);
            }
        }
        #endregion 
    }
}
