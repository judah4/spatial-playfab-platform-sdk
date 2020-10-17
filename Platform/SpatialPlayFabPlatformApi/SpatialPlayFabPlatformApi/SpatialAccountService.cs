using Improbable.SpatialOS.Platform.Common;
using PlayFab.ServerModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpatialPlayFabPlatformApi
{
    public static class SpatialAccountService
    {
        public class TokenData
        {
            public string Token { get; set; }
            public DateTime TimeFound { get; set; }
        }


        static TokenData Token;

        public static PlatformRefreshTokenCredential GetPlatformRefreshToken()
        {
            var task = Task.Run(() => GetPlatformRefreshTokenAsync());
            task.Wait();
            return task.Result;
        }

        public static async Task<PlatformRefreshTokenCredential> GetPlatformRefreshTokenAsync()
        {
            if (NeedToken())
            {
                await GetToken();
            }

            return new PlatformRefreshTokenCredential(Token.Token);
        }

        static bool NeedToken()
        {
            if (Token == null)
                return true;

            if (Token.TimeFound < DateTime.UtcNow.AddHours(-2))
                return true;

            return false;
        }

        static async Task GetToken()
        {
            Token = null;

            var tokenData = await PlayFab.PlayFabServerAPI.GetTitleInternalDataAsync(new GetTitleDataRequest()
            {
                Keys = new List<string>() {"SpatialosPlatformToken"}
            });

            if (tokenData.Error != null)
            {
                throw new Exception(tokenData.Error.GenerateErrorReport());
            }

            string token = null;

            if (!tokenData.Result.Data.TryGetValue("SpatialosPlatformToken", out token))
            {
                throw new Exception("Token does not exist in playfab title data.");
            }

            Token = new TokenData() {Token = token, TimeFound = DateTime.UtcNow,};

        }
    }
}
