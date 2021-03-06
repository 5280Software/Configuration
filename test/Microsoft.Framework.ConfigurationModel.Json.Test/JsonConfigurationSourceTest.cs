// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

using Resources = Microsoft.Framework.ConfigurationModel.Json.Resources;

namespace Microsoft.Framework.ConfigurationModel
{
    public class JsonConfigurationSourceTest
    {
        private static readonly string ArbitraryFilePath = "Unit tests do not touch file system";

        [Fact]
        public void LoadKeyValuePairsFromValidJson()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"
{
    'firstname': 'test',
    'test.last.name': 'last.name',
        'residential.address': {
            'street.name': 'Something street',
            'zipcode': '12345'
        }
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

            jsonConfigSrc.Load();

            Assert.Equal("test", jsonConfigSrc.Get("firstname"));
            Assert.Equal("last.name", jsonConfigSrc.Get("test.last.name"));
            Assert.Equal("Something street", jsonConfigSrc.Get("residential.address:STREET.name"));
            Assert.Equal("12345", jsonConfigSrc.Get("residential.address:zipcode"));
        }

        [Fact]
        public void LoadMethodCanHandleEmptyValue()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"
{
    'name': ''
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

            jsonConfigSrc.Load();

            Assert.Equal(string.Empty, jsonConfigSrc.Get("name"));
        }

        [Fact]
        public void NonObjectRootIsInvalid()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"'test'";
            var jsonConfigSource = new JsonConfigurationSource(streamHandler, json);
            var expectedMsg = Resources.FormatError_RootMustBeAnObject(string.Empty, 1, 6);

            var exception = Assert.Throws<FormatException>(() => jsonConfigSource.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void SupportAndIgnoreComments()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"/* Comments */
                {/* Comments */
                ""name"": /* Comments */ ""test"",
                ""address"": {
                    ""street"": ""Something street"", /* Comments */
                    ""zipcode"": ""12345""
                }
            }";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

            jsonConfigSrc.Load();

            Assert.Equal("test", jsonConfigSrc.Get("name"));
            Assert.Equal("Something street", jsonConfigSrc.Get("address:street"));
            Assert.Equal("12345", jsonConfigSrc.Get("address:zipcode"));
        }

        [Fact]
        public void ArraysAreNotSupported()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
                'name': 'test',
                'address': ['Something street', '12345']
            }";
            var jsonConfigSource = new JsonConfigurationSource(streamHandler, json);
            var expectedMsg = Resources.FormatError_UnsupportedJSONToken("StartArray", "address", 3, 29);

            var exception = Assert.Throws<FormatException>(() => jsonConfigSource.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenUnexpectedEndFoundBeforeFinishParsing()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
                'name': 'test',
                'address': {
                    'street': 'Something street',
                    'zipcode': '12345'
                }
            /* Missing a right brace here*/";
            var jsonConfigSource = new JsonConfigurationSource(streamHandler, json);
            var expectedMsg = Resources.FormatError_UnexpectedEnd("address", 7, 44);

            var exception = Assert.Throws<FormatException>(() => jsonConfigSource.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new JsonConfigurationSource(null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new JsonConfigurationSource(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

		[Fact]
		public void ThrowExceptionWhenPassingNullAsStreamHandler()
		{
			var expectedMsg = new ArgumentException(Resources.Error_InvalidStreamHandler, "streamHandler").Message;

			var exception = Assert.Throws<ArgumentException>(() => new JsonConfigurationSource(null, ArbitraryFilePath));

			Assert.Equal(expectedMsg, exception.Message);
		}

		[Fact]
        public void ThrowExceptionWhenKeyIsDuplicated()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
                'name': 'test',
                'address': {
                    'street': 'Something street',
                    'zipcode': '12345'
                },
                'name': 'new name'
            }";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

            var exception = Assert.Throws<FormatException>(() => jsonConfigSrc.Load());

            Assert.Equal(Resources.FormatError_KeyIsDuplicated("name"), exception.Message);
        }

        [Fact]
        public void CommitMethodPreservesCommments()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
  ""name"": ""test"",
  ""address"": {
    ""street"": ""Something street"",
    ""zipcode"": ""12345""
  }
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

			jsonConfigSrc.Load();

            jsonConfigSrc.Commit();

            var newContents = StreamToString(streamHandler.Stream);

            Assert.Equal(json, newContents);
        }

        [Fact]
        public void CommitMethodUpdatesValues()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
  ""name"": ""test"",
  ""address"": {
    ""street"": ""Something street"",
    ""zipcode"": ""12345""
  }
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

			jsonConfigSrc.Load();

            jsonConfigSrc.Set("name", "new_name");

            jsonConfigSrc.Set("address:zipcode", "67890");

            jsonConfigSrc.Commit();

            var newContents = StreamToString(streamHandler.Stream);

            Assert.Equal(json.Replace("test", "new_name").Replace("12345", "67890"), newContents);
        }

        [Fact]
        public void CommitMethodCanHandleEmptyValue()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
  ""key1"": """",
  ""key2"": {
    ""key3"": """"
  }
}";
            var expectedJson = @"{
  ""key1"": ""value1"",
  ""key2"": {
    ""key3"": ""value2""
  }
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

			jsonConfigSrc.Load();
            jsonConfigSrc.Set("key1", "value1");
            jsonConfigSrc.Set("key2:key3", "value2");

            jsonConfigSrc.Commit();

            var newContents = StreamToString(streamHandler.Stream);

            Assert.Equal(expectedJson, newContents);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenFindInvalidModificationAfterLoadOperation()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
  ""name"": ""test"",
  ""address"": {
    ""street"": ""Something street"",
    ""zipcode"": ""12345""
  }
}";
            var modifiedJson = @"
{
  ""name"": [""first"", ""last""],
  ""address"": {
    ""street"": ""Something street"",
    ""zipcode"": ""12345""
  }
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

			jsonConfigSrc.Load();

            var exception = Assert.Throws<FormatException>(() => jsonConfigSrc.Commit());

            Assert.Equal(Resources.FormatError_UnsupportedJSONToken("StartArray", "name", 3, 12), exception.Message);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenFindNewlyAddedKeyAfterLoadOperation()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
  ""name"": ""test"",
  ""address"": {
    ""street"": ""Something street"",
    ""zipcode"": ""12345""
  }
}";
            var newJson = @"{
  ""name"": ""test"",
  ""address"": {
    ""street"": ""Something street"",
    ""zipcode"": ""12345""
  },
  ""NewKey"": ""NewValue""
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

            jsonConfigSrc.Load();

            var exception = Assert.Throws<InvalidOperationException>(() => jsonConfigSrc.Commit());

            Assert.Equal(Resources.FormatError_CommitWhenNewKeyFound("NewKey"), exception.Message);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenKeysAreMissingInConfigFile()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var json = @"{
  ""name"": ""test"",
  ""address"": {
    ""street"": ""Something street"",
    ""zipcode"": ""12345""
  }
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, json);

			jsonConfigSrc.Load();
            json = json.Replace(@"""name"": ""test"",", string.Empty);

            var exception = Assert.Throws<InvalidOperationException>(() => jsonConfigSrc.Commit());

            Assert.Equal(Resources.FormatError_CommitWhenKeyMissing("name"), exception.Message);
        }

        [Fact]
        public void CanCreateNewConfig()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var targetJson = @"{
  ""name"": ""test"",
  ""address:street"": ""Something street"",
  ""address:zipcode"": ""12345""
}";
            var jsonConfigSrc = new JsonConfigurationSource(streamHandler, String.Empty);

			jsonConfigSrc.Set("name", "test");
            jsonConfigSrc.Set("address:street", "Something street");
            jsonConfigSrc.Set("address:zipcode", "12345");

            jsonConfigSrc.Commit();

            Assert.Equal(targetJson, StreamToString(streamHandler.Stream));
        }

        private static Stream StringToStream(string str)
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write(str);
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            return memStream;
        }

        private static string StreamToString(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
