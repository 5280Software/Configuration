// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.ConfigurationModel
{
    public static class XmlConfigurationExtension
    {
        public static IConfigurationSourceContainer AddXmlFile(this IConfigurationSourceContainer configuration, string path,
			IConfigurationStreamHandler streamHandler = null)
        {
			if (streamHandler == null)
				configuration.Add(new XmlConfigurationSource(path));
			else
				configuration.Add(new XmlConfigurationSource(streamHandler, path));

			return configuration;
        }
    }
}
