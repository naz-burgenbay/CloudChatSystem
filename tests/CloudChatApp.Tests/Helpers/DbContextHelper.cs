using CloudChatApp.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudChatApp.Tests.Helpers
{
    public static class DbContextHelper
    {
        public static ApplicationDbContext CreateInMemoryContext()
        {
            return CreateInMemoryContext(Guid.NewGuid().ToString());
        }

        public static ApplicationDbContext CreateInMemoryContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            return new ApplicationDbContext(options);
        }
    }
}
