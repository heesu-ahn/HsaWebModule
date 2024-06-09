using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.AccessControl;
using System.Text;

namespace HsaWebModule.ProgramUtil
{
    public class UserDataModule
    {
        string parentPath = string.Empty;

        public Dictionary<string, string> changeUserInfo;

        public UserDataModule(string parentFolderPath) 
        {
            parentPath = parentFolderPath;
        }
        public void CreateUserInfoData() 
        {
            DirectoryInfo di;
            string userInfoPath = Path.Combine(parentPath, "Account");
            Dictionary<string, string> defaultUserInfo = new Dictionary<string, string>();
            string defaultsha256Password = Program.encryptModule.GenerateSymmetricKey("12345");
            defaultUserInfo.Add(Program.defaultUserName, defaultsha256Password);
            
            if (!Directory.Exists(userInfoPath))
            {
                di = Directory.CreateDirectory(userInfoPath);
                DirectorySecurity security = new DirectorySecurity();
                security = di.GetAccessControl();
                security.AddAccessRule(new FileSystemAccessRule(Environment.UserName, FileSystemRights.FullControl, AccessControlType.Allow));
                di.SetAccessControl(security);

                string userInfoFilePath = Path.Combine(di.FullName, "userInfo.dat");
                File.Create(userInfoFilePath).Close();
                string contents = JsonConvert.SerializeObject(defaultUserInfo);

                var data = Encoding.UTF8.GetBytes(contents);
                Byte[] compressedByte;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress))
                    {
                        ds.Write(data, 0, data.Length);
                        ds.Close();
                    }

                    compressedByte = ms.ToArray();
                    ms.Close();
                }
                File.WriteAllBytes(userInfoFilePath, compressedByte);
                HsaWebModule.Default.CurrentUserName = defaultUserInfo.Keys.ToArray()[0];
                HsaWebModule.Default.Save();

                Program.log.Debug(string.Format("현재 사용자 정보 : {0}", HsaWebModule.Default.CurrentUserName));
            }
            else
            {
                if (!File.Exists(userInfoPath + "/userInfo.dat"))
                {
                    di = Directory.CreateDirectory(Path.Combine(userInfoPath, "Account"));
                    DirectorySecurity security = new DirectorySecurity();
                    security = di.GetAccessControl();
                    security.AddAccessRule(new FileSystemAccessRule(Environment.UserName, FileSystemRights.FullControl, AccessControlType.Allow));
                    di.SetAccessControl(security);

                    string userInfoFilePath = Path.Combine(di.FullName, "userInfo.dat");
                    File.Create(userInfoFilePath).Close();
                    string contents = JsonConvert.SerializeObject(defaultUserInfo);

                    var data = Encoding.UTF8.GetBytes(contents);
                    Byte[] compressedByte;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress))
                        {
                            ds.Write(data, 0, data.Length);
                            ds.Close();
                        }

                        compressedByte = ms.ToArray();
                        ms.Close();
                    }
                    File.WriteAllBytes(userInfoFilePath, compressedByte);
                    HsaWebModule.Default.CurrentUserName = defaultUserInfo.Keys.ToArray()[0];
                    HsaWebModule.Default.Save();
                }
                Program.log.Debug(string.Format("현재 사용자 정보 : {0}", HsaWebModule.Default.CurrentUserName));
            }
        }

        public string GetUserInfoData() 
        {
            
            string result = string.Empty;
            MemoryStream zipMemoryStream = new MemoryStream();
            Stream entryStream = File.Open(Program.certFilePath + "/Account/userInfo.dat", FileMode.Open);
            entryStream.CopyTo(zipMemoryStream);
            byte[] zipFileByte = zipMemoryStream.ToArray(); 
            MemoryStream resultStream = new MemoryStream();

            using (MemoryStream ms = new MemoryStream(zipFileByte))
            {
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    ds.CopyTo(resultStream);
                    ds.Close();
                }
            }
            byte[] decompressedByte = resultStream.ToArray();
            zipMemoryStream.Close();
            resultStream.Close();
            entryStream.Close();
            result = Encoding.UTF8.GetString(decompressedByte);
            //Console.WriteLine(result);
            return result;
        }

        public void UpdateUserInfoData(string updateCurrentUserName) 
        {
            if (changeUserInfo != null)
            {
                string changeFilePath = Program.certFilePath;
                if (Directory.Exists(changeFilePath))
                {
                    changeFilePath = Path.Combine(changeFilePath, "Account") + "/userInfo.dat";
                    if (File.Exists(changeFilePath))
                    {
                        File.Delete(changeFilePath);
                        File.Create(changeFilePath).Close();
                        string contents = JsonConvert.SerializeObject(changeUserInfo);

                        var data = Encoding.UTF8.GetBytes(contents);
                        Byte[] compressedByte;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress))
                            {
                                ds.Write(data, 0, data.Length);
                                ds.Close();
                            }

                            compressedByte = ms.ToArray();
                            ms.Close();
                        }
                        File.WriteAllBytes(changeFilePath, compressedByte);

                        Console.WriteLine(GetUserInfoData());
                        HsaWebModule.Default.CurrentUserName = updateCurrentUserName;
                        HsaWebModule.Default.Save();
                        changeUserInfo = null;
                    }
                }
            }
        }
    }
}
