
namespace HmiTesting.Web.Navigation
{
    using System.Threading.Tasks;
    using HmiTesting.Core.DTOs;
    using HmiTesting.Core.Interfaces;

    public static class HmiPageTaskExtensions
    {
        /// <summary>
        /// awaits the task and returns the area layout content A of the page.
        /// </summary>
        public static async Task<IHmiPageArea> GetAreaLayoutListA(
            this Task<IHmiPage> pageTask,
            int number)
        {
            var page = await pageTask.ConfigureAwait(false);
            return page.GetAreaLayoutListA(number);
        }

        /// <summary>
        /// awaits the task and returns the area layout content A of the page.
        /// </summary>
        public static async Task<IHmiPageArea> GetAreaLayoutContentB(
            this Task<IHmiPage> pageTask,
            int number)
        {
            var page = await pageTask.ConfigureAwait(false);
            return page.GetAreaLayoutContentB(number);
        }

        public static async Task<T> GetElementByName<T>(
        this Task<IHmiPageArea> areaTask,
        string name) where T : IInputControl
        {
            var area = await areaTask.ConfigureAwait(false);
            return area.getElementByName<T>(name);
        }
    }
}
