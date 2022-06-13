using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ViewModel;

namespace Services
{
    public interface IDatabaseService
    {
        StateViewModel<List<FundEstimateModel>> GetFundEstimate();
        Task SetSentStatus(StateViewModel<List<FundEstimateModel>> stateViewModel);
    }
    internal class DatabaseService : IDatabaseService
    {
        private static IDbConnection _db = new SqlConnection();
        private IConfiguration _configuration { get; }
        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        public StateViewModel<List<FundEstimateModel>> GetFundEstimate()
        {
            StateViewModel<List<FundEstimateModel>> state = new StateViewModel<List<FundEstimateModel>>();
            try
            {
                _db.ConnectionString = _configuration.GetConnectionString("default");
                var value = (_db.Query<FundEstimateModel>(
                    @"SELECT * FROM FundEstimate T where T.FundEstimateLogId = ( select Max(Id) from FundEstimateLog WHERE Status = 'Processed')", new { }, commandType: CommandType.Text, commandTimeout: 500)).ToList();
                state.Code = 200;
                state.Msg = "Success";
                state.Response = value;
            }
            catch
            {
                throw;
            }
            return state;
        }

        public async Task SetSentStatus(StateViewModel<List<FundEstimateModel>> stateViewModel)
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
                catch
                {
                    throw;
                }
            }
        }
    }
}