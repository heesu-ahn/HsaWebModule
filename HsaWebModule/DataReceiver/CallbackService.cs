using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HsaWebModule.DataReceiver
{
    public class CallbackService
    {
        public CallbackService() { }

        delegate JwtSecurityToken decryptDelegate(string tokenString, string secretKey);
        public async Task<JwtSecurityToken> DecryptJwt(string tokenString, string secretKey)
        {
            SecurityToken returnResult;
            decryptDelegate jwtDelegate = new decryptDelegate(DecyrptJwtAction);
            IAsyncResult asyncRes = jwtDelegate.BeginInvoke(tokenString,secretKey, null, null);
            returnResult = jwtDelegate.EndInvoke(asyncRes);
            return returnResult as JwtSecurityToken;
        }
        public JwtSecurityToken DecyrptJwtAction(string tokenString, string secretKey)
        {
            JwtSecurityToken result = null;
            try
            {
                result = Program.encryptModule.isValidToken(tokenString, secretKey);
            }
            catch (Exception)
            {
                return result;
            }
            return result;
        }

        delegate string userDtaParsingDelegate(Dictionary<string, string> getMessage,string currentUser, string userInfoData, string socketId);
        public async Task<string> ParsingUserDta(Dictionary<string, string> getMessage, string currentUser, string userInfoData, string socketId)
        {
            string returnResult = string.Empty;
            userDtaParsingDelegate parsingDelegate = new userDtaParsingDelegate(ParsingDtaAction);
            IAsyncResult asyncRes = parsingDelegate.BeginInvoke(getMessage, currentUser, userInfoData, socketId, null, null);
            returnResult = parsingDelegate.EndInvoke(asyncRes);
            return returnResult;
        }
        public string ParsingDtaAction(Dictionary<string, string> getMessage, string currentUser, string userInfoData, string socketId)
        {
            string result = string.Empty;
            try
            {
                if (!getMessage.ContainsKey("password"))
                {
                    if (Program.webSocketService.gServer.WebSocketServices["/"].Sessions.ActiveIDs.Contains(socketId))
                    {
                        Program.WriteLog("인가되지 않은 사용자 입니다.");
                        Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("인가되지 않은 사용자 입니다.", socketId);
                        Program.webSocketService.gServer.WebSocketServices["/"].Sessions.CloseSession(socketId);
                    }
                    return result;
                }
                else 
                {
                    if (string.IsNullOrEmpty(userInfoData))
                    {
                        if (Program.webSocketService.gServer.WebSocketServices["/"].Sessions.ActiveIDs.Contains(socketId))
                        {
                            Program.WriteLog("사용자 정보가 없습니다.");
                            Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("사용자 정보가 없습니다.", socketId);
                            Program.webSocketService.gServer.WebSocketServices["/"].Sessions.CloseSession(socketId);
                        }
                        return result;
                    }
                    Dictionary<string, string> userInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(userInfoData);
                    if (!userInfo.ContainsKey(currentUser))
                    {
                        if (Program.webSocketService.gServer.WebSocketServices["/"].Sessions.ActiveIDs.Contains(socketId))
                        {
                            Program.WriteLog("인가되지 않은 사용자 입니다.");
                            Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("인가되지 않은 사용자 입니다.", socketId);
                            Program.webSocketService.gServer.WebSocketServices["/"].Sessions.CloseSession(socketId);
                        }
                        return result;
                    }
                    // SHA256 대칭키로 변환
                    if (getMessage["password"].StartsWith("value&")) {
                        getMessage["password"] = Encoding.UTF8.GetString(Convert.FromBase64String(getMessage["password"].Replace("value&", "")));
                    }

                    getMessage["password"] = Program.encryptModule.GenerateSymmetricKey(getMessage["password"].ToString());
                    if (!userInfo[currentUser].Equals(getMessage["password"].ToString()))
                    {
                        if (Program.webSocketService.gServer.WebSocketServices["/"].Sessions.ActiveIDs.Contains(socketId))
                        {
                            Program.WriteLog("인가되지 않은 사용자 입니다.");
                            Program.webSocketService.gServer.WebSocketServices["/"].Sessions.SendTo("인가되지 않은 사용자 입니다.", socketId);
                            Program.webSocketService.gServer.WebSocketServices["/"].Sessions.CloseSession(socketId);
                        }
                        return result;
                    }
                    else 
                    {
                        // password Check Success
                        result = getMessage["password"].ToString();
                    }
                }
            }
            catch (Exception)
            {
                return result;
            }
            return result;
        }
    }
}
