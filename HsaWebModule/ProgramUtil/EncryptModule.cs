using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace HsaWebModule.ProgramUtil
{
    public  class EncryptModule
    {
        public string GenerateJWTToken(IEnumerable<Claim> claimEnumerable, string secretKey, string issuer, string audience)
        {
            string tokenString = string.Empty;
            try
            {
                byte[] keyByteArray = Encoding.UTF8.GetBytes(secretKey);
                SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(keyByteArray);
                SigningCredentials credentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
                
                JwtSecurityToken token = new JwtSecurityToken
                (
                    issuer: issuer,
                    audience: audience,
                    claims: claimEnumerable,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: credentials
                );

                tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                Program.WriteLog(ex.Message,true);
            }
            return tokenString;
        }

        public JwtSecurityToken isValidToken(string signedAndEncodedToken, string secretKey)
        {
            byte[] keyByteArray = Encoding.UTF8.GetBytes(secretKey);
            SymmetricSecurityKey signingKey = new SymmetricSecurityKey(keyByteArray);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters()
            {
                ValidAudiences = new string[]{HsaWebModuleProperty.mainProperty.audience },
                ValidIssuers = new string[]{ Program.issuer },
                IssuerSigningKey = signingKey
            };
            SecurityToken validatedToken;
            tokenHandler.ValidateToken(signedAndEncodedToken,tokenValidationParameters, out validatedToken);
            return validatedToken as JwtSecurityToken;
        }

        private RijndaelManaged myRijndael;
        public static string salt = RandomGenerateSalt();
        public static string initialisationVector = RandaomGenerateIv();
        public static string passPhrase = HsaWebModule.Default.CurrentUserName;

        private static string RandomGenerateSalt()
        {
            var random = new RNGCryptoServiceProvider();
            // Maximum length of salt
            int max_length = 32;
            // Empty salt array
            byte[] salt = new byte[max_length];
            // Build the random bytes
            random.GetNonZeroBytes(salt);
            // Return the string encoded salt
            return BitConverter.ToString(salt).Replace("-", "");
        }
        private static string RandaomGenerateIv()
        {
            var random = new RNGCryptoServiceProvider();
            // Maximum length of salt
            int max_length = 16;
            // Empty salt array
            byte[] salt = new byte[max_length];
            // Build the random bytes
            random.GetNonZeroBytes(salt);
            // Return the string encoded salt
            return BitConverter.ToString(salt).Replace("-", "");
        }



        private byte[] CreateKey(string s, string p)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(p);
            byte[] saltBytes = HexStringToByte(s);
            var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 11);
            return key.GetBytes(16);
        }
        public string DecryptText(string encryptedString)
        {
            try
            {
                using (myRijndael = new RijndaelManaged())
                {
                    //key,iv 값의 인코딩방식에 따라 byte변환을 달리해야한다
                    myRijndael.Key = CreateKey(salt, passPhrase);
                    myRijndael.IV = HexStringToByte(initialisationVector);
                    myRijndael.Mode = CipherMode.CBC;
                    myRijndael.Padding = PaddingMode.PKCS7;

                    Byte[] ourEnc = Convert.FromBase64String(encryptedString);
                    string ourDec = DecryptStringFromBytes(ourEnc, myRijndael.Key, myRijndael.IV);

                    return ourDec;
                }
            }
            catch (Exception e)
            {
                Program.WriteLog(e.Message,true);
                return encryptedString;
            }
        }
        //암호화 
        public string EncryptText(string encryptedString)
        {
            try
            {
                using (myRijndael = new RijndaelManaged())
                {
                    //key,iv 값의 인코딩방식에 따라 byte변환을 달리해야한다
                    myRijndael.Key = CreateKey(salt, passPhrase);
                    myRijndael.IV = HexStringToByte(initialisationVector);
                    myRijndael.Mode = CipherMode.CBC;
                    myRijndael.Padding = PaddingMode.PKCS7;

                    Byte[] ourEnc = Encoding.UTF8.GetBytes(encryptedString);
                    string ourDec = EncryptStringFromBytes(ourEnc, myRijndael.Key, myRijndael.IV);
                    return ourDec;
                }
            }
            catch (Exception e)
            {
                Program.WriteLog(e.Message,true);
                return encryptedString;
            }
        }

        //Byte를 EncryptString으로 변환 
        private string EncryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msEncrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srEncrypt = new StreamReader(csEncrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            MemoryStream ms = new MemoryStream();
                            srEncrypt.BaseStream.CopyTo(ms);
                            plaintext = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
            }
            return plaintext;
        }

        //Byte를 복호화된 스트링으로 변환
        private string DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an RijndaelManaged object 
            // with the specified key and IV. 
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }

        //BASE64 인코딩된 키 , IV 값 Byte변환
        private static byte[] Base64StringToByte(string base64String)
        {
            try
            {
                return Convert.FromBase64String(salt);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        //UTF8 인코딩된 키 , IV 값 Byte변환
        private static byte[] Utf8StringToByte(string utf8String)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(utf8String);
                return bytes;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        //HexString KEY , IV 값 Byte변환
        private static byte[] HexStringToByte(string hexString)
        {
            try
            {
                int bytesCount = (hexString.Length) / 2;
                byte[] bytes = new byte[bytesCount];
                for (int x = 0; x < bytesCount; ++x)
                {
                    bytes[x] = Convert.ToByte(hexString.Substring(x * 2, 2), 16);
                }
                return bytes;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        public string GetFileHash(string from = "")
        {
            string hashData = "";
            string fileDirectory = string.IsNullOrEmpty(from) ? "" : from;
            if (Directory.Exists(fileDirectory))
            {
                DirectoryInfo di = new DirectoryInfo(fileDirectory);
                foreach (FileInfo fi in di.GetFiles())
                {
                    if (fi.Name.Equals(Program.propertyConfigName+".xml")) 
                    {
                        byte[] btAscii = File.ReadAllBytes(fi.FullName);
                        byte[] btHash = MD5.Create().ComputeHash(btAscii);
                        hashData = BitConverter.ToString(btHash);
                    }
                }
            }
            return hashData;
        }

        public string GenerateSymmetricKey(string inputPassword) 
        {
            SHA256Managed sha256Managed = new SHA256Managed();
            byte[] encryptBytes = sha256Managed.ComputeHash(Encoding.UTF8.GetBytes(inputPassword));
            return Convert.ToBase64String(encryptBytes);
        }

    }
}
