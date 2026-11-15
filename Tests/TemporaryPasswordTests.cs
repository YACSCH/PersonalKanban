using PersonalKanban.Services;
using Xunit;

namespace PersonalKanban.Tests
{
    public class TemporaryPasswordTests
    {
        [Fact]
        public void Cumple_los_requisitos_de_contrasena()
        {
            for (var i = 0; i < 50; i++)
            {
                var password = TemporaryPassword.Create();

                Assert.Equal(16, password.Length);
                Assert.Contains(password, char.IsUpper);
                Assert.Contains(password, char.IsLower);
                Assert.Contains(password, char.IsDigit);
                Assert.Contains(password, c => !char.IsLetterOrDigit(c));
            }
        }
    }
}
