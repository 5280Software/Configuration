// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || ASPNET50 || ASPNETCORE50
using System;
using System.IO;

namespace Microsoft.Framework.ConfigurationModel
{
	public class StringConfigurationStreamHandler : IConfigurationStreamHandler
	{
		public Stream Stream { get; protected set; }

		public virtual Stream CreateStream(string path)
		{
			// Simply read stream as the stream is not persisted.
			return Stream = ReadStream(path);
		}

		public virtual void DeleteStream(string path)
		{
			Stream = null;
		}

		public virtual bool DoesStreamExist(string path)
		{
			return !String.IsNullOrEmpty(path) && Stream != null;
		}

		public virtual Stream ReadStream(string path)
		{
			var stream = new MemoryStream();

			var writer = new StreamWriter(stream);

			writer.Write(path);

			writer.Flush();

			stream.Seek(0, SeekOrigin.Begin);

			return stream;
		}

		public virtual void WriteStream(Stream stream, string path)
		{
			using (var outputStream = Stream)
			{
				stream.CopyTo(outputStream);
			}
		}
	}
}
#endif