using Newtonsoft.Json;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace HsaWebModule.DataReceiver
{
    public class PreCheckHandler
    {
        CallbackService callbackService;
        private string secretKey = string.Empty;
        private string savedJwt = string.Empty;
        private string getJwt = string.Empty;

        public PreCheckHandler(string connsaveJwt,string inputJwt, string secKey) 
        {
            callbackService = new CallbackService();
            savedJwt = connsaveJwt;
            getJwt = inputJwt;
            secretKey = secKey;
        }

        public JwtSecurityToken AuthTokenCheck() 
        {
            JwtSecurityToken token = new JwtSecurityToken();
            if (!string.IsNullOrEmpty(getJwt)) 
            {
                getJwt = Program.encryptModule.DecryptText(getJwt);
                if (savedJwt != getJwt)
                {
                    Program.WriteLog("JWT 정보가 변경 되었습니다.");
                    return token;
                }
                else 
                {
                    token = callbackService.DecryptJwt(getJwt,secretKey).Result;
                    Program.WriteLog("JWT AES 복호화 완료. : " + JsonConvert.SerializeObject(token.Payload));
                }
            }
            return token;
        }

        public bool PasswordVaildCheck(Dictionary<string, string> getMessage, string currentUser, string userInfoData, string socketId) 
        {
            bool result = false;
            string pwtCheckValue = callbackService.ParsingDtaAction(getMessage,currentUser,userInfoData,socketId);
            result = !string.IsNullOrEmpty(pwtCheckValue);
            return result;
        }
    }
}
