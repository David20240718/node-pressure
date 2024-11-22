using AElf;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Cryptography.SecretSharing;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Org.BouncyCastle.Utilities.Encoders;
using Shouldly;
using Volo.Abp.Threading;

namespace PackageTest;

[TestClass]
public class Netstand2_1Test
{
    private ILog Logger { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        Log4NetHelper.LogInit(".NET Stand 2.1 Test");
        Logger = Log4NetHelper.GetLogger();
    }

    [TestMethod]
    public void AddressCreateTest()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        Logger.Info(keyPair.PrivateKey.ToHex());
        Logger.Info(keyPair.PublicKey.ToHex());

        var keyPairFromPrivate = CryptoHelper.FromPrivateKey(keyPair.PrivateKey);
        keyPair.PrivateKey.ShouldBe(keyPairFromPrivate.PrivateKey);
        keyPair.PublicKey.ShouldBe(keyPairFromPrivate.PublicKey);

        var data = HashHelper.ComputeFrom("TEST").ToByteArray();
        var sign = CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, data);
        var verifySignature = CryptoHelper.VerifySignature(sign, data, keyPair.PublicKey);
        verifySignature.ShouldBeTrue();
        var recoverPublicKey = CryptoHelper.RecoverPublicKey(sign, data, out var publicKey);
        recoverPublicKey.ShouldBeTrue();
        publicKey.ShouldBe(keyPair.PublicKey);
    }
    
    [TestMethod]
    public void AddressVerifyTest()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        Logger.Info(keyPair.PrivateKey.ToHex());
        Logger.Info(keyPair.PublicKey.ToHex());

        var address = Address.FromPublicKey(keyPair.PublicKey);
        var addressToBase58 = address.ToBase58();
        var addressVerifyBase58 = AddressHelper.VerifyFormattedAddress(addressToBase58);
        addressVerifyBase58.ShouldBeTrue();
        
        var addressFromBase58 = Address.FromBase58(addressToBase58);
        addressFromBase58.ShouldBe(address);
    }

    [TestMethod]
    public void AddressCreateAndSendTransaction()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        Logger.Info(keyPair.PrivateKey.ToHex());
        Logger.Info(keyPair.PublicKey.ToHex());
    }
    
    [TestMethod]
    [DataRow("12345678")]
    public void NewAccount(string password = "")
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        var accountManager = new AccountManager();
        var newAccount = accountManager.NewAccount(password, keyPair);
        Logger.Info($"{keyPair.PublicKey.ToHex()}");
    }

    [TestMethod]
    public void GetAccount(string privateKey, string exceptAddress)
    {
        var accountPrivateKey = "07bc54c5e977d735a53501a32d9d6c3d70acf9831d8bba1ee43fa18011007bc6";
        var keyPairFromPrivate = CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(accountPrivateKey));
        var address = Address.FromPublicKey(keyPairFromPrivate.PublicKey);
        var addressToBase58 = address.ToBase58();
        var addressVerifyBase58 = AddressHelper.VerifyFormattedAddress(addressToBase58);
        addressVerifyBase58.ShouldBeTrue();
        Logger.Info(addressToBase58);
    }

    [TestMethod]
    [DataRow("AELF")]
    [DataRow("tDVV")]
    [DataRow("tDVW")]
    public void TestBase58(string chainId)
    {
        var intChainId = ChainHelper.ConvertBase58ToChainId(chainId);
        var base58ChainId = ChainHelper.ConvertChainIdToBase58(intChainId);
        // var getChainId = ChainHelper.GetChainId(1);
        
        // intChainId.ShouldBe(getChainId);
        base58ChainId.ShouldBe(chainId);
        Logger.Info($"{base58ChainId} {intChainId}");
    }

    [TestMethod]
    public void TestHashType()
    {
        var s = "3cd3fb2f114983b518445cefa83952442b024ba00d6a15698fea1e09ff439dda";
        var sToHash = Hash.LoadFromHex(s);
        var byteArray = ByteArrayHelper.HexStringToByteArray(s);
        var byteArrayToHash = Hash.LoadFromByteArray(byteArray);
        var base64 = Base64.ToBase64String(byteArray);
        var base64ToHash = Hash.LoadFromBase64(base64);

        var hashToString = sToHash.ToHex();
        var hashToByteArray = sToHash.ToByteArray();
        
        // var hashToInt64 = sToHash.ToInt64();
        // var hashToIntToByte = hashToInt64.ToBytes();
        // var int64ToHash = Hash.LoadFromByteArray(hashToIntToByte);
            
        sToHash.ShouldBe(byteArrayToHash);
        sToHash.ShouldBe(base64ToHash);
        
        hashToString.ShouldBe(s);
        hashToByteArray.ShouldBe(byteArray);
        // int64ToHash.ShouldBe(sToHash);
    }
    
    
    [TestMethod]
    public void TestHashType_2()
    {
        var s = HashHelper.ComputeFrom("TEST");
        var hashToString = s.ToHex();
        var byteArray = ByteArrayHelper.HexStringToByteArray(hashToString);
        var byteArrayToHash = Hash.LoadFromByteArray(byteArray);
        var base64 = Base64.ToBase64String(byteArray);
        var base64ToHash = Hash.LoadFromBase64(base64);

        var hashToByteArray = s.ToByteArray();
        
        // var hashToInt64 = s.ToInt64();
        // var hashToIntToByte = hashToInt64.ToBytes();
        // var int64ToHash = Hash.LoadFromByteArray(hashToIntToByte);
            
        s.ShouldBe(byteArrayToHash);
        s.ShouldBe(base64ToHash);
        
        hashToByteArray.ShouldBe(byteArray);
        // int64ToHash.ShouldBe(sToHash);
    }

    [TestMethod]
    public void TestBigIntType()
    {
        var s = "1000000000000000000000000000000";
        var bigIntString = new BigIntValue(s);
        bigIntString.Value.ShouldBe(s);
        
        int a = 10;
        var bigIntInt = new BigIntValue(a);
        bigIntInt.Value.ShouldBe(a.ToString());
        
        long b = 1000000_00000000;
        var bigIntLong = new BigIntValue(b);
        bigIntLong.Value.ShouldBe(b.ToString());

        Int32Value c = new Int32Value { Value = a };
        var bigIntInt32Value = new BigIntValue(c);
        bigIntInt32Value.Value.ShouldBe(c.ToString());
        
        Int64Value d = new Int64Value { Value = b };
        var bigIntInt64Value = new BigIntValue(d);
        bigIntInt64Value.Value.ShouldBe(d.Value.ToString());
    }
    
    [TestMethod]
    public void TestBigIntOperator()
    {
        var s1 = "1000000000000000000000000000000";
        var s2 = "1000000000000000000000000000001";
        
        var bigIntString1 = new BigIntValue(s1);
        var bigIntString2 = new BigIntValue(s2);

        (bigIntString1 < bigIntString2).ShouldBeTrue();
        (bigIntString1 > bigIntString2).ShouldBeFalse();
        
        bigIntString2.Add(bigIntString1).ShouldBe(new BigIntValue("2000000000000000000000000000001"));
        bigIntString2.Sub(bigIntString1).ShouldBe(new BigIntValue("1"));
        bigIntString1.Mul(new BigIntValue(2)).ShouldBe(new BigIntValue("2000000000000000000000000000000"));
        bigIntString1.Div(new BigIntValue(2)).ShouldBe(new BigIntValue("500000000000000000000000000000"));
        bigIntString1.Pow(2).ShouldBe(new BigIntValue("1000000000000000000000000000000000000000000000000000000000000"));
    }

    [TestMethod]
    public void SecretSharingTest()
    {
        var message = HashHelper.ComputeFrom("message").ToByteArray();
        var minimumCount = 5.Mul(2).Div(3);
        var secrets =
            SecretSharingHelper.EncodeSecret(message, minimumCount, 5);
        var encryptedValues = new Dictionary<string, byte[]>();
        var decryptedValues = new Dictionary<string, byte[]>();
        var ownerKeyPair = CryptoHelper.GenerateKeyPair();
        var othersKeyPairs = new List<ECKeyPair>
        {
            CryptoHelper.GenerateKeyPair(),
            CryptoHelper.GenerateKeyPair(),
            CryptoHelper.GenerateKeyPair(),
            CryptoHelper.GenerateKeyPair()
        };
        var decryptResult = new byte[0];

        var initial = 0;
        foreach (var keyPair in othersKeyPairs)
        {
            var encryptedMessage = CryptoHelper.EncryptMessage(ownerKeyPair.PrivateKey, keyPair.PublicKey,
                secrets[initial++]);
            encryptedValues.Add(keyPair.PublicKey.ToHex(), encryptedMessage);
        }

        // Check encrypted values.
        encryptedValues.Count.ShouldBe(4);

        // Others try to recover.
        foreach (var keyPair in othersKeyPairs)
        {
            var cipherMessage = encryptedValues[keyPair.PublicKey.ToHex()];
            var decryptMessage =
                CryptoHelper.DecryptMessage(ownerKeyPair.PublicKey, keyPair.PrivateKey, cipherMessage);
            decryptedValues.Add(keyPair.PublicKey.ToHex(), decryptMessage);

            if (decryptedValues.Count >= minimumCount)
            {
                decryptResult = SecretSharingHelper.DecodeSecret(
                    decryptedValues.Values.ToList(),
                    Enumerable.Range(1, minimumCount).ToList(), minimumCount);
                break;
            }
        }
        decryptResult.ShouldBe(message);
    }
    
    [TestMethod]
    public void SecretSharingTest_FromRealData()
    {
        var minimumCount = 5.Mul(2).Div(3);
        var encryptedPieces = new List<string>
        { 
            "MIIBgQIBADCCAXoGCSqGSIb3DQEHA6CCAWswggFnAgECMYIBODCCATQCAQKgCgQIA6EFd+F5op8wEAYHKoZIzj0CAQYFK4EEAAoEggEPMIIBCwIBADBWMBAGByqGSM49AgEGBSuBBAAKA0IABC5iJ2K6OkTqTtR5yx4XUPHym/coyrWELhl0wYy33sBHraRw7X4gvod8BEMiGB0CQTTliPmotF6/tSSRwQ57i8QwGAYHKIGMcQIFAjANBglghkgBZQMEAgIFADBBMA0GCWCGSAFlAwQCAgUABDBHT06z4o6AhTvR+1s/dR8hUbBbMpxOxmB4nChpOwduhGuZ+fiGzMXGXiFtIez/LtUwUTAdBglghkgBZQMEASoEEGT0KAL3uaNTwYLmUnhxOAEEMP457cRyZCgW0A6lZsiKlADFotKhF1fpeFwQk9Hr2G0PMBBiOYijYCnYW4FOHHnTjTAmBgkqhkiG9w0BBwEwGQYJYIZIAWUDBAEuBAw914RfhbamHNjb55F9Q7epK9ydrEiG6Ol55yiaRTRGiV7ctn0rkJMckbg2d5spdsllvwfKm1GyzukaV22t", 
            "MIIBgQIBADCCAXoGCSqGSIb3DQEHA6CCAWswggFnAgECMYIBODCCATQCAQKgCgQIsjeT4h2m3awwEAYHKoZIzj0CAQYFK4EEAAoEggEPMIIBCwIBADBWMBAGByqGSM49AgEGBSuBBAAKA0IABOfJqUpODjNXGdFjSYG3MN8AjkVUD3Fc1TYu8Qc2AQYa1UkRmj+3uGJT1jSE5GUwaaB8qGNfCPQBQr9UmOJtDQIwGAYHKIGMcQIFAjANBglghkgBZQMEAgIFADBBMA0GCWCGSAFlAwQCAgUABDCCmtmFHJxmyIw1nc4MNYbIm0sh94Z2cg/goAygGFX4Xt5dDJv3fLFtHJkx+jhAYgIwUTAdBglghkgBZQMEASoEEPpI/awBh2iHDD3FXNF9k8QEMCmUvkJArHOyNcdMEMe8FQhNlnbuFisiKKVjgo6T/N3JygiTXQtJN/E0OprkkPUFIzAmBgkqhkiG9w0BBwEwGQYJYIZIAWUDBAEuBAw/2+SDJ8nfZ7MLf3IAXYEaeo5cyOooL0x4zXFVbQz4LAobtXw9JQ/MFf00EZWPklUHb02aAt8c17cZKj3c", 
            "MIIBgQIBADCCAXoGCSqGSIb3DQEHA6CCAWswggFnAgECMYIBODCCATQCAQKgCgQIzKVwNH6qab8wEAYHKoZIzj0CAQYFK4EEAAoEggEPMIIBCwIBADBWMBAGByqGSM49AgEGBSuBBAAKA0IABEuGpdmRUeR5LYhiXzLNbvP3o7opvUymPcWFb32sxBZUY7P0mm3zEH+MXUfTPnPM9SYSn2Csw+0nQvn135ukKukwGAYHKIGMcQIFAjANBglghkgBZQMEAgIFADBBMA0GCWCGSAFlAwQCAgUABDDudzGlkHFIZPktEH6NR1OI+1CtZfWtPP9Eg3HWDsR9q/PPK52SRKTlMiKZ+G/FJB8wUTAdBglghkgBZQMEASoEEH5ggn9xElI2i/0GBkIY15oEMB9N6eoJDYsAc98pwYLrz0L/tMKsfaLiQEzSgXbRPvki9riyX1fCl5fR31O1BMl9fzAmBgkqhkiG9w0BBwEwGQYJYIZIAWUDBAEuBAyFeeVFQJmIAfkXcfAPzIkdQFVU9N4OPEH95y4NDrJvtzwn1TqDCDlT1gYW4Y14o5/A+AQB7LR0FlTzVmwB",
            "MIIBgQIBADCCAXoGCSqGSIb3DQEHA6CCAWswggFnAgECMYIBODCCATQCAQKgCgQIa9ZPpYI2Sy8wEAYHKoZIzj0CAQYFK4EEAAoEggEPMIIBCwIBADBWMBAGByqGSM49AgEGBSuBBAAKA0IABCjh1fmsiUp8vx3uD2Nxrdy/QCC1ztnoMX9xbUATaqqa1Kn/NEW/Rl4iU4QxpfBy9FAv96KZfl/upU2nrfey+GgwGAYHKIGMcQIFAjANBglghkgBZQMEAgIFADBBMA0GCWCGSAFlAwQCAgUABDCpyUTO3+Xzn1qhSrPhcldxhfV3LhK5++LAG+rPKticZWMmIViHSqYjPM1VjIypN0swUTAdBglghkgBZQMEASoEENSUdK/X3gqrBR45Q0djst0EMF8S8C6MCPePupxgI6PU2fsZzW68L7CyvutZ0i5WRxOrVV7AwirPOrOQ4BJ7D4PqbDAmBgkqhkiG9w0BBwEwGQYJYIZIAWUDBAEuBAyxnUE6sik4sOAClM/foGwNRaoMl0zaElca4z68dFsBicPac6JT6gLudxeGY9hmzE1UgcOLAaj2Faom7CdB",
            "MIIBgQIBADCCAXoGCSqGSIb3DQEHA6CCAWswggFnAgECMYIBODCCATQCAQKgCgQIR6r14R5D5XwwEAYHKoZIzj0CAQYFK4EEAAoEggEPMIIBCwIBADBWMBAGByqGSM49AgEGBSuBBAAKA0IABB5QOVqPrk9P6uwYdwlVswGhABrzes3MVPhYK4mCZHNNMKDfxlvrd7d9Yxq2lsYWtMOwbuR4F6tsV8iIbo1UAhQwGAYHKIGMcQIFAjANBglghkgBZQMEAgIFADBBMA0GCWCGSAFlAwQCAgUABDB5BqDskwbeE07ViVV+VRCbvw14Rd+gAjCg0lTtwX0ROS5/xOHAJaHiGGGWcyHTmqswUTAdBglghkgBZQMEASoEEO6QyOuTH+y9XYhm9jGj/AQEMPRIQ93tTjYoAJCSt5cZUyFKZfigs+2U+C9cvrnOzdkbSZUMbkW7SFJB/ujHFVPVmzAmBgkqhkiG9w0BBwEwGQYJYIZIAWUDBAEuBAwwvscsQeUvAYLXGVDQiaqo+i30KLUnZEvyTEa8Nz4MP6jRW0MqGS+yIGEYGaO7HDV5PjA4ja34ibxTDAaA"
        };
        
        var owner = "CmjUNFhh2yFGVVXMxxy5q8Ki23tDqSnFbvEzc3Xz5UL3Aktav";
        var ownerPrivate = "4ca47ccb990c1a50cc6d7278bdce64c9f71adf32be2d07c811fbe2da2de044f4";
        var ownerKeyPairFromPrivate = CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(ownerPrivate));
        var decryptedValues = new Dictionary<string, byte[]>();

        var keyPairs =  new List<ECKeyPair>
        {
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray("")),
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray("")),
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray("")),
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray("")),
            CryptoHelper.FromPrivateKey(ByteArrayHelper.HexStringToByteArray(""))
        };

        for (int i = 0; i < encryptedPieces.Count; i++)
        {
            var cipherMessage = ByteString.FromBase64(encryptedPieces[i]).ToByteArray();
            var decryptMessage =
                CryptoHelper.DecryptMessage(ownerKeyPairFromPrivate.PublicKey, keyPairs[i].PrivateKey, cipherMessage);
            decryptedValues.Add(keyPairs[i].PublicKey.ToHex(), decryptMessage);
        }

        var decryptResult = SecretSharingHelper.DecodeSecret(
            decryptedValues.Values.ToList(),
            Enumerable.Range(1, minimumCount).ToList(), minimumCount);
        Logger.Info(decryptResult.ToHex());
    }
}