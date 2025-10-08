using External.FakeStore.Models;

namespace External.FakeStore.Mappers
{
    public static class FakeStoreCartMapper
    {
        public static FakeStoreCartCreateRequest ToCreateRequest(int userId, List<FakeStoreCartProduct> products)
        {
            return new FakeStoreCartCreateRequest
            {
                UserId = userId,
                Products = products ?? new List<FakeStoreCartProduct>()
            };
        }

        public static FakeStoreCartUpdateRequest ToUpdateRequest(int id, int userId, List<FakeStoreCartProduct> products)
        {
            return new FakeStoreCartUpdateRequest
            {
                Id = id,
                UserId = userId,
                Products = products ?? new List<FakeStoreCartProduct>()
            };
        }
    }
}