// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

using Resources = Microsoft.Framework.ConfigurationModel.Xml.Resources;

namespace Microsoft.Framework.ConfigurationModel
{
    public class XmlConfigurationSourceTest
    {
        private static readonly string ArbitraryFilePath = "Unit tests do not touch file system";

        [Fact]
        public void LoadKeyValuePairsFromValidXml()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml = @"
                <settings>
                    <Data.Setting>
                        <DefaultConnection>
                            <Connection.String>Test.Connection.String</Connection.String>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data.Setting>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("Test.Connection.String", xmlConfigSrc.Get("DATA.SETTING:DEFAULTCONNECTION:CONNECTION.STRING"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("DATA.SETTING:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("data.setting:inventory:connectionstring"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data.setting:Inventory:Provider"));
        }

        [Fact]
        public void LoadMethodCanHandleEmptyValue()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Key1></Key1>
    <Key2 Key3="""" />
</settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal(string.Empty, xmlConfigSrc.Get("Key1"));
            Assert.Equal(string.Empty, xmlConfigSrc.Get("Key2:Key3"));
        }

        [Fact]
        public void CommonAttributesContributeToKeyValuePairs()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
@"<settings Port=""8008"">
    <Data>
        <DefaultConnection
            ConnectionString=""TestConnectionString""
            Provider=""SqlClient""/>
        <Inventory
            ConnectionString=""AnotherTestConnectionString""
            Provider=""MySql""/>
    </Data>
</settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("8008", xmlConfigSrc.Get("Port"));
            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportMixingChildElementsAndAttributes()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<settings Port='8008'>
                    <Data>
                        <DefaultConnection Provider='SqlClient'>
                            <ConnectionString>TestConnectionString</ConnectionString>
                        </DefaultConnection>
                        <Inventory ConnectionString='AnotherTestConnectionString'>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("8008", xmlConfigSrc.Get("Port"));
            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void NameAttributeContributesToPrefix()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<settings>
                    <Data Name='DefaultConnection'>
                        <ConnectionString>TestConnectionString</ConnectionString>
                        <Provider>SqlClient</Provider>
                    </Data>
                    <Data Name='Inventory'>
                        <ConnectionString>AnotherTestConnectionString</ConnectionString>
                        <Provider>MySql</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void NameAttributeInRootElementContributesToPrefix()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<settings Name='Data'>
                    <DefaultConnection>
                        <ConnectionString>TestConnectionString</ConnectionString>
                        <Provider>SqlClient</Provider>
                    </DefaultConnection>
                    <Inventory>
                        <ConnectionString>AnotherTestConnectionString</ConnectionString>
                        <Provider>MySql</Provider>
                    </Inventory>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportMixingNameAttributesAndCommonAttributes()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<settings>
                    <Data Name='DefaultConnection'
                          ConnectionString='TestConnectionString'
                          Provider='SqlClient' />
                    <Data Name='Inventory' ConnectionString='AnotherTestConnectionString'>
                          <Provider>MySql</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportCDATAAsTextNode()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<settings>
                    <Data>
                        <Inventory>
                            <Provider><![CDATA[SpecialStringWith<>]]></Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("SpecialStringWith<>", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportAndIgnoreComments()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<!-- Comments --> <settings>
                    <Data> <!-- Comments -->
                        <DefaultConnection>
                            <ConnectionString><!-- Comments -->TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings><!-- Comments -->";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportAndIgnoreXMLDeclaration()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<?xml version='1.0' encoding='UTF-8'?>
                <settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportAndIgnoreProcessingInstructions()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<?xml version='1.0' encoding='UTF-8'?>
                <?xml-stylesheet type='text/xsl' href='style1.xsl'?>
                    <settings>
                        <?xml-stylesheet type='text/xsl' href='style2.xsl'?>
                        <Data>
                            <DefaultConnection>
                                <ConnectionString>TestConnectionString</ConnectionString>
                                <Provider>SqlClient</Provider>
                            </DefaultConnection>
                            <Inventory>
                                <ConnectionString>AnotherTestConnectionString</ConnectionString>
                                <Provider>MySql</Provider>
                            </Inventory>
                        </Data>
                    </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

            xmlConfigSrc.Load();

            Assert.Equal("TestConnectionString", xmlConfigSrc.Get("Data:DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", xmlConfigSrc.Get("Data:DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", xmlConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", xmlConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void ThrowExceptionWhenFindDTD()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<!DOCTYPE DefaultConnection[
                    <!ELEMENT DefaultConnection (ConnectionString,Provider)>
                    <!ELEMENT ConnectionString (#PCDATA)>
                    <!ELEMENT Provider (#PCDATA)>
                ]>
                <settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);
            var expectedMsg = "For security reasons DTD is prohibited in this XML document. "
                + "To enable DTD processing set the DtdProcessing property on XmlReaderSettings "
                + "to Parse and pass the settings into XmlReader.Create method.";

            var exception = Assert.Throws<System.Xml.XmlException>(() => xmlConfigSrc.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenFindNamespace()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<settings xmlns:MyNameSpace='http://microsoft.com/wwa/mynamespace'>
                    <MyNameSpace:Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                        <Inventory>
                            <ConnectionString>AnotherTestConnectionString</ConnectionString>
                            <Provider>MySql</Provider>
                        </Inventory>
                    </MyNameSpace:Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);
            var expectedMsg = Resources.FormatError_NamespaceIsNotSupported(Resources.FormatMsg_LineInfo(1, 11));

            var exception = Assert.Throws<FormatException>(() => xmlConfigSrc.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new XmlConfigurationSource(null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(Resources.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new XmlConfigurationSource(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

		[Fact]
		public void ThrowExceptionWhenPassingNullAsStreamHandler()
		{
			var expectedMsg = new ArgumentException(Resources.Error_InvalidStreamHandler, "streamHandler").Message;

			var exception = Assert.Throws<ArgumentException>(() => new XmlConfigurationSource(null, ArbitraryFilePath));

			Assert.Equal(expectedMsg, exception.Message);
		}

		[Fact]
        public void ThrowExceptionWhenKeyIsDuplicated()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml =
                @"<settings>
                    <Data>
                        <DefaultConnection>
                            <ConnectionString>TestConnectionString</ConnectionString>
                            <Provider>SqlClient</Provider>
                        </DefaultConnection>
                    </Data>
                    <Data Name='DefaultConnection' ConnectionString='NewConnectionString'>
                        <Provider>NewProvider</Provider>
                    </Data>
                </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);
            var expectedMsg = Resources.FormatError_KeyIsDuplicated("Data:DefaultConnection:ConnectionString",
                Resources.FormatMsg_LineInfo(8, 52));

            var exception = Assert.Throws<FormatException>(() => xmlConfigSrc.Load());

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void CommitMethodPreservesCommmentsAndProcessingInstructionsAndWhiteSpaces()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
                    <settings>
                        <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
                        <Data>
                            <DefaultConnection>
                                <ConnectionString>TestConnectionString</ConnectionString>
                                <Provider>SqlClient</Provider>
                            </DefaultConnection>
                            <Inventory>
                                <ConnectionString>AnotherTestConnectionString</ConnectionString>
                                <Provider>MySql</Provider>
                            </Inventory>
                        </Data>
                    </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

			xmlConfigSrc.Load();

            xmlConfigSrc.Commit();

            var newContents = StreamToString(streamHandler.Stream);
            Assert.Equal(xml, newContents);
        }

        [Fact]
        public void CommitMethodUpdatesValues()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Data>
        <DefaultConnection>
            <ConnectionString>TestConnectionString</ConnectionString>
            <Provider>SqlClient</Provider>
        </DefaultConnection>
        <Inventory>
            <ConnectionString>AnotherTestConnectionString</ConnectionString>
            <Provider>MySql</Provider>
        </Inventory>
    </Data>
</settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);
            xmlConfigSrc.Load();
            xmlConfigSrc.Set("Data:DefaultConnection:Provider", "NewSqlClient");
            xmlConfigSrc.Set("Data:Inventory:Provider", "NewMySql");

            xmlConfigSrc.Commit();

            var newContents = StreamToString(streamHandler.Stream);
            Assert.Equal(xml.Replace("SqlClient", "NewSqlClient").Replace("MySql", "NewMySql"), newContents);
        }

        [Fact]
        public void CommitMethodCanHandleEmptyValue()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Key1></Key1>
    <Key2 Key3="""" />
</settings>";
            var expectedXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Key1>Value1</Key1>
    <Key2 Key3=""Value2"" />
</settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

			xmlConfigSrc.Load();
            xmlConfigSrc.Set("Key1", "Value1");
            xmlConfigSrc.Set("Key2:Key3", "Value2");

            xmlConfigSrc.Commit();

            var newContents = StreamToString(streamHandler.Stream);
            Assert.Equal(expectedXml, newContents);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenFindInvalidModificationAfterLoadOperation()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
                    <settings>
                        <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
                        <Data>
                            <DefaultConnection>
                                <ConnectionString>TestConnectionString</ConnectionString>
                                <Provider>SqlClient</Provider>
                            </DefaultConnection>
                            <Inventory>
                                <ConnectionString>AnotherTestConnectionString</ConnectionString>
                                <Provider>MySql</Provider>
                            </Inventory>
                        </Data>
                    </settings>";
            var modifiedXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                    <?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
                    <settings xmlns:MyNameSpace=""http://microsoft.com/wwa/mynamespace"">
                        <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
                        <MyNameSpace:Data>
                            <DefaultConnection>
                                <ConnectionString>TestConnectionString</ConnectionString>
                                <Provider>SqlClient</Provider>
                            </DefaultConnection>
                            <Inventory>
                                <ConnectionString>AnotherTestConnectionString</ConnectionString>
                                <Provider>MySql</Provider>
                            </Inventory>
                        </MyNameSpace:Data>
                    </settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

			xmlConfigSrc.Load();

            var exception = Assert.Throws<FormatException>(() => xmlConfigSrc.Commit());

            Assert.Equal(Resources.FormatError_NamespaceIsNotSupported(Resources.FormatMsg_LineInfo(3, 31)),
                exception.Message);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenFindNewlyAddedKeyAfterLoadOperation()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Data>
        <DefaultConnection>
            <ConnectionString>TestConnectionString</ConnectionString>
            <Provider>SqlClient</Provider>
        </DefaultConnection>
        <Inventory>
            <ConnectionString>AnotherTestConnectionString</ConnectionString>
            <Provider>MySql</Provider>
        </Inventory>
    </Data>
</settings>";
            var modifiedXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Data>
        <DefaultConnection>
            <ConnectionString>TestConnectionString</ConnectionString>
            <Provider>SqlClient</Provider>
            <NewKey>NewValue</NewKey>
        </DefaultConnection>
        <Inventory>
            <ConnectionString>AnotherTestConnectionString</ConnectionString>
            <Provider>MySql</Provider>
        </Inventory>
    </Data>
</settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

			xmlConfigSrc.Load();

            var exception = Assert.Throws<InvalidOperationException>(() => xmlConfigSrc.Commit());

            Assert.Equal(
                Resources.FormatError_CommitWhenNewKeyFound("Data:DefaultConnection:NewKey"), exception.Message);
        }

        [Fact]
        public void CommitOperationThrowsExceptionWhenKeysAreMissingInConfigFile()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Data>
        <DefaultConnection>
            <ConnectionString>TestConnectionString</ConnectionString>
            <Provider>SqlClient</Provider>
        </DefaultConnection>
        <Inventory>
            <ConnectionString>AnotherTestConnectionString</ConnectionString>
            <Provider>MySql</Provider>
        </Inventory>
    </Data>
</settings>";
            var modifiedXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<?xml-stylesheet type=""text/xsl"" href=""style1.xsl""?>
<settings>
    <?xml-stylesheet type=""text/xsl"" href=""style2.xsl""?>
    <Data>
        <DefaultConnection>
            <ConnectionString>TestConnectionString</ConnectionString>
        </DefaultConnection>
        <Inventory>
            <Provider>MySql</Provider>
        </Inventory>
    </Data>
</settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, xml);

			xmlConfigSrc.Load();

            var exception = Assert.Throws<InvalidOperationException>(() => xmlConfigSrc.Commit());

            Assert.Equal(
                Resources.
                FormatError_CommitWhenKeyMissing("Data:DefaultConnection:Provider, Data:Inventory:ConnectionString"),
                exception.Message);
        }

        [Fact]
        public void CanCreateNewConfig()
		{
			var streamHandler = new StringConfigurationStreamHandler();

			var targetXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<settings>
  <Key1 Name=""Key2:Key3"">Value1</Key1>
  <Key4>Value2</Key4>
</settings>";
            var xmlConfigSrc = new XmlConfigurationSource(streamHandler, String.Empty);

			xmlConfigSrc.Set("Key1:Key2:Key3", "Value1");
            xmlConfigSrc.Set("Key4", "Value2");

            xmlConfigSrc.Commit();

            var newContents = StreamToString(streamHandler.Stream);
            Assert.Equal(targetXml, newContents);
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
