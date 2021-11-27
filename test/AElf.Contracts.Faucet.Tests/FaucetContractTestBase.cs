using AElf.Boilerplate.TestBase;
using AElf.Cryptography.ECDSA;

namespace AElf.Contracts.Faucet
{
    public class FaucetContractTestBase : DAppContractTestBase<FaucetContractTestModule>
    {
        // You can get address of any contract via GetAddress method, for example:
        // internal Address DAppContractAddress => GetAddress(DAppSmartContractAddressNameProvider.StringName);

        internal FaucetContractContainer.FaucetContractStub GetFaucetContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<FaucetContractContainer.FaucetContractStub>(DAppContractAddress, senderKeyPair);
        }
    }
}