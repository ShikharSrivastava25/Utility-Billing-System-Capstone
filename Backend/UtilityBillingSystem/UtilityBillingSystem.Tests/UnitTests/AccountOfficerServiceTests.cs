using UtilityBillingSystem.Services;
using UtilityBillingSystem.Tests.UnitTests.Base;
using Xunit;

namespace UtilityBillingSystem.Tests.UnitTests
{
    public class AccountOfficerServiceTests : BaseServiceTest
    {
        private readonly AccountOfficerService _service;

        public AccountOfficerServiceTests() : base()
        {
            _service = new AccountOfficerService(Context, Mapper);
        }

    }
}

