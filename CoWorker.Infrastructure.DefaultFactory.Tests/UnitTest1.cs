using System;
using System.Linq;
using Xunit;

namespace CoWorker.Infrastructure.DefaultFactory
{
    public class UnitTest1
    {
        public static DefaultFactory InitFactory(DateTime dt)
        {
            var factory = new DefaultFactory();
            factory.Init<Item>("now", q => q.Where(x => x.Format == "yyyy/MM/dd HH:mm:ss")
                 .Where(x => x.Tick == dt.Ticks));
            factory.Init<Model>(q => q.Where(x => x.Item.Equals(factory.Get<Item>())));
            return factory;
        }
        [Fact]
        public void test_inner_factory_getter()
        {
            var dt = new DateTime();
            var factory = InitFactory(dt);
            var init = factory.Get<Model>();
            Assert.Equal(string.Empty,init.Item.Format);
            Assert.Equal(dt.Ticks,init.Item.Tick);
        }

        [Fact]
        public void test_use_exist_object()
        {
            var dt = DateTime.Now;
            var factory = InitFactory(dt);
            var now = factory.Get<Model>("now");
            Assert.Equal("yyyy/MM/dd HH:mm:ss", now.Item.Format);
            Assert.Equal(dt.Ticks, now.Item.Tick);
        }
        [Fact]
        public void test_ref_same()
        {
            var dt = DateTime.Now;
            var factory = InitFactory(dt);
            var now = factory.Get<Model>("now");
            var now2 = factory.Get<Model>("now");
            Assert.Same(now, now2);
        }
    }

    public class Model
    {
        public Item Item { get; set; }
        public string GetDateTime() => new DateTime(Item.Tick).ToString(Item.Format);
    }

    public class Item
    {
        public string Format { get; set; }
        public long Tick { get; set; }
    }
}
