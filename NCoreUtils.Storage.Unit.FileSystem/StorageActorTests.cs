using System;
using Xunit;

namespace NCoreUtils.Storage.Unit
{
    public class StorageActorTests
    {
        [Fact]
        public void NullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => StorageActor.Group(null));
            Assert.Throws<ArgumentNullException>(() => StorageActor.User(null));

            Assert.Throws<ArgumentException>(() => StorageActor.Group(string.Empty));
            Assert.Throws<ArgumentException>(() => StorageActor.User(string.Empty));
        }

        [Fact]
        public void HashCode()
        {
            Assert.Equal(StorageActor.Public.GetHashCode(), StorageActor.Public.GetHashCode());
            Assert.Equal(StorageActor.Authenticated.GetHashCode(), StorageActor.Authenticated.GetHashCode());
            Assert.Equal(StorageActor.User("x").GetHashCode(), StorageActor.User("x").GetHashCode());
            Assert.Equal(StorageActor.Group("x").GetHashCode(), StorageActor.Group("x").GetHashCode());
            Assert.Equal(-1, new StorageActor((StorageActorType) 16, null).GetHashCode());
        }

        [Fact]
        public void Equality()
        {
            Assert.Equal(StorageActor.Public, StorageActor.Public);
            Assert.Equal(StorageActor.Authenticated, StorageActor.Authenticated);
            Assert.Equal(StorageActor.User("x"), StorageActor.User("x"));
            Assert.Equal(StorageActor.Group("x"), StorageActor.Group("x"));

            Assert.NotEqual(StorageActor.Public, StorageActor.Authenticated);
            Assert.NotEqual(StorageActor.Public, StorageActor.User("x"));
            Assert.NotEqual(StorageActor.Public, StorageActor.Group("x"));

            Assert.NotEqual(StorageActor.Authenticated, StorageActor.Public);
            Assert.NotEqual(StorageActor.Authenticated, StorageActor.User("x"));
            Assert.NotEqual(StorageActor.Authenticated, StorageActor.Group("x"));

            Assert.NotEqual(StorageActor.User("x"), StorageActor.Public);
            Assert.NotEqual(StorageActor.User("x"), StorageActor.Authenticated);
            Assert.NotEqual(StorageActor.User("x"), StorageActor.User("y"));
            Assert.NotEqual(StorageActor.User("x"), StorageActor.Group("x"));

            Assert.NotEqual(StorageActor.Group("x"), StorageActor.Public);
            Assert.NotEqual(StorageActor.Group("x"), StorageActor.Authenticated);
            Assert.NotEqual(StorageActor.Group("x"), StorageActor.User("x"));
            Assert.NotEqual(StorageActor.Group("x"), StorageActor.Group("y"));

            // invalid
            Assert.NotEqual(new StorageActor((StorageActorType) 16, null), new StorageActor((StorageActorType) 16, null));
        }

        [Fact]
        public void ObjectEquality()
        {
            object @public = StorageActor.Public;
            object authenticated = StorageActor.Authenticated;
            object userX = StorageActor.User("x");
            object groupX = StorageActor.Group("x");

            Assert.True(@public.Equals(@public));
            Assert.True(authenticated.Equals(authenticated));
            Assert.True(userX.Equals(userX));
            Assert.True(groupX.Equals(groupX));

            Assert.False(@public.Equals(authenticated));
            Assert.False(@public.Equals(userX));
            Assert.False(@public.Equals(groupX));

            Assert.False(authenticated.Equals(@public));
            Assert.False(authenticated.Equals(userX));
            Assert.False(authenticated.Equals(groupX));
            Assert.False(userX.Equals(@public));
            Assert.False(userX.Equals(authenticated));
            Assert.False(userX.Equals(StorageActor.User("y")));
            Assert.False(userX.Equals(groupX));
            Assert.False(groupX.Equals(@public));
            Assert.False(groupX.Equals(authenticated));
            Assert.False(groupX.Equals(userX));
            Assert.False(groupX.Equals(StorageActor.Group("y")));

            // invalid
            Assert.False(((object)new StorageActor((StorageActorType) 16, null)).Equals(new StorageActor((StorageActorType) 16, null)));
            Assert.False(authenticated.Equals(2));
        }
    }
}