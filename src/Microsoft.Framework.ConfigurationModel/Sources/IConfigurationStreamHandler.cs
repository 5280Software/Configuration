// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || ASPNET50 || ASPNETCORE50
using System;
using System.IO;

namespace Microsoft.Framework.ConfigurationModel
{
	public interface IConfigurationStreamHandler
	{
		Stream CreateStream(string path);

		void DeleteStream(string path);

		bool DoesStreamExist(string path);

		Stream ReadStream(string path);

		void WriteStream(Stream stream, string path);
	}
}
#endif