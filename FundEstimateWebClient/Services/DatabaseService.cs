using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using ViewModel;

namespace Services
{
    class DatabaseService : HelperService
    {
        private static IDbConnection _db = new SqlConnection();

        public DatabaseService(string connstring)
        {
            _db = new SqlConnection(connstring);
        }

        public async Task<StateViewModel<List<FundEstimateModel>>> GetFundEstimates()
        {
            StateViewModel<List<FundEstimateModel>> state = new StateViewModel<List<FundEstimateModel>>();
            try
            {
                var value = (await _db.QueryAsync<FundEstimateModel>(@"SELECT FE.* FROM FundEstimate FE 
                            where FE.FundEstimateLogId=(select min(FEL.Id) from FundEstimateLog FEL WHERE FEL.Status = 'Processed') ", new { },
                                 commandType: CommandType.Text, commandTimeout: 500).ConfigureAwait(false)).ToList();

                state.Code = 200;
                state.Msg = "Success";
                state.Response = value;
            }
            catch (Exception msg)
            {
                state.Code = 500;
                state.Msg = msg.Message;
            }
            return state;
        }

        internal async Task SetSentStatus(StateViewModel<List<FundEstimateModel>> stateViewModel)
        {
            var FundEstimateLogId = stateViewModel.Response.FirstOrDefault().FundEstimateLogId;
            if (FundEstimateLogId > 0)
            {
                try
                {
                    string sql = @"UPDATE FundEstimateLog set Status=@Status, SentDate=GETDATE() WHERE Status='Processed' AND ID=@ID;";
                    var fundEstimateModel = await _db.ExecuteAsync(sql, new
                    {
                        Status = "Sent",
                        ID = FundEstimateLogId
                    }, commandType: CommandType.Text);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
    }
}
