using System.Threading.Tasks;

namespace HmiTesting.Web.Components
{
    public static class LabelExtensions
    {

        public static async Task<string> GetText(this Task<Label> labelTask)
        {
            var label = await labelTask.ConfigureAwait(false);
            return await label.GetText().ConfigureAwait(false);
        }
    }
}

