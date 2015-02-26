// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || ASPNET50 || ASPNETCORE50
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Framework.ConfigurationModel
{
    public abstract class BaseStreamConfigurationSource : ConfigurationSource, ICommitableConfigurationSource
    {
		protected readonly IConfigurationStreamHandler streamHandler;

		public BaseStreamConfigurationSource(IConfigurationStreamHandler streamHandler, string path)
        {
			// Path can be empty as some Configuration Stream Handler implementations 
			// may be able to handle empty string, but still won't support null path
			if (path == null)
			{
				throw new ArgumentException(Resources.Error_InvalidFilePath, "path");
			}

			Path = PathResolver.ResolveAppRelativePath(path);

			if (streamHandler == null)
				throw new ArgumentException(Resources.Error_InvalidStreamHandler, "streamHandler");

			this.streamHandler = streamHandler;
		}

		public string Path { get; private set; }

		public override void Load()
        {
            using (var stream = streamHandler.ReadStream(Path))
            {
                Load(stream);
            }
        }
        
        public virtual void Commit()
        {
            // If the config stream is empty
            // i.e. we don't have a template to follow when generating contents of new config file
            if (streamHandler.DoesStreamExist(Path))
            {
                var newConfigFileStream = streamHandler.CreateStream(Path);

                try
                {
                    // Generate contents and write it to the newly created config file
                    GenerateNewConfig(newConfigFileStream);

					streamHandler.WriteStream(newConfigFileStream, Path);
                }
                catch
                {
                    newConfigFileStream.Dispose();

					// The operation should be atomic because we don't want a corrupted config file
					// So we roll back if the operation fails
					if (streamHandler.DoesStreamExist(Path))
					{
						streamHandler.DeleteStream(Path);
					}

					// Rethrow the exception
					throw;
                }
                finally
                {
                    newConfigFileStream.Dispose();
                }

                return;
            }

            // Because we need to read the original contents while generating new contents, the new contents are
            // cached in memory and used to overwrite original contents after we finish reading the original contents
            using (var cacheStream = new MemoryStream())
            {
                using (var inputStream = streamHandler.ReadStream(Path))
                {
                    Commit(inputStream, cacheStream);
                }

                // Use the cached new contents to overwrite original contents
                cacheStream.Seek(0, SeekOrigin.Begin);

				streamHandler.WriteStream(cacheStream, Path);
            }
        }

		protected abstract void Load(Stream stream);

		// Use the original file as a template while generating new file contents
		// to make sure the format is consistent and comments are not lost
		protected abstract void Commit(Stream inputStream, Stream outputStream);

		// Write the contents of newly created config file to given stream
		protected abstract void GenerateNewConfig(Stream outputStream);
    }
}
#endif
