// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || ASPNET50 || ASPNETCORE50
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Framework.ConfigurationModel
{
    public class IniFileConfigurationSource : BaseStreamConfigurationSource, ICommitableConfigurationSource
    {
        // http://en.wikipedia.org/wiki/INI_file
        /// <summary>
        /// Files are simple line structures
        /// [Section:Header]
        /// key1=value1
        /// key2 = " value2 "
        /// ; comment
        /// # comment
        /// / comment
        /// </summary>
        /// <param name="path">The path and file name to load.</param>
        public IniFileConfigurationSource(string path)
			: this(new FileConfigurationStreamHandler(), path)
        { }

		/// <summary>
		/// Files are simple line structures
		/// [Section:Header]
		/// key1=value1
		/// key2 = " value2 "
		/// ; comment
		/// # comment
		/// / comment
		/// </summary>
		/// <param name="streamHandler">The stream handler.</param>
		/// <param name="path">The path and file name to load.</param>
		public IniFileConfigurationSource(IConfigurationStreamHandler streamHandler, string path)
			: base(streamHandler, path)
		{ }
		
        protected override void Load(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var reader = new StreamReader(stream))
            {
                var sectionPrefix = string.Empty;

                while (reader.Peek() != -1)
                {
                    var rawLine = reader.ReadLine();
                    var line = rawLine.Trim();

                    // Ignore blank lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    // Ignore comments
                    if (line[0] == ';' || line[0] == '#' || line[0] == '/')
                    {
                        continue;
                    }
                    // [Section:header] 
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        // remove the brackets
                        sectionPrefix = line.Substring(1, line.Length - 2) + ":";
                        continue;
                    }

                    // key = value OR "value"
                    int separator = line.IndexOf('=');
                    if (separator < 0)
                    {
                        throw new FormatException(Resources.FormatError_UnrecognizedLineFormat(rawLine));
                    }

                    string key = sectionPrefix + line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();

                    // Remove quotes
                    if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (data.ContainsKey(key))
                    {
                        throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                    }

                    data[key] = value;
                }
            }

            Data = data;
        }

		// Use the original file as a template while generating new file contents
		// to make sure the format is consistent and comments are not lost
		protected override void Commit(Stream inputStream, Stream outputStream)
        {
            var processedKeys = new HashSet<string>();
            var outputWriter = new StreamWriter(outputStream);

            using (var inputReader = new StreamReader(inputStream))
            {
                var sectionPrefix = string.Empty;

                while (inputReader.Peek() != -1)
                {
                    var rawLine = inputReader.ReadLine();
                    var line = rawLine.Trim();

                    // Is this the last line?
                    var lineEnd = inputReader.Peek() == -1 ? string.Empty : Environment.NewLine;

                    // Ignore blank lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        outputWriter.Write(rawLine + lineEnd);
                        continue;
                    }
                    // Ignore comments
                    if (line[0] == ';' || line[0] == '#' || line[0] == '/')
                    {
                        outputWriter.Write(rawLine + lineEnd);
                        continue;
                    }
                    // [Section:header] 
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        outputWriter.Write(rawLine + lineEnd);

                        // remove the brackets
                        sectionPrefix = line.Substring(1, line.Length - 2) + ":";
                        continue;
                    }

                    // key = value OR "value"
                    int separator = line.IndexOf('=');
                    if (separator < 0)
                    {
                        throw new FormatException(Resources.FormatError_UnrecognizedLineFormat(rawLine));
                    }

                    var key = sectionPrefix + line.Substring(0, separator).Trim();
                    var value = line.Substring(separator + 1).Trim();

                    // Output preserves white spaces in original file
                    int rawSeparator = rawLine.IndexOf('=');
                    var outKeyStr = rawLine.Substring(0, rawSeparator);
                    var outValueStr = rawLine.Substring(rawSeparator + 1);

                    // Remove quotes
                    if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (!Data.ContainsKey(key))
                    {
                        throw new InvalidOperationException(Resources.FormatError_CommitWhenNewKeyFound(key));
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        outValueStr = Data[key];
                    }
                    else
                    {
                        outValueStr = outValueStr.Replace(value, Data[key]);
                    }

                    outputWriter.Write(string.Format("{0}={1}{2}", outKeyStr, outValueStr, lineEnd));

                    processedKeys.Add(key);
                }

                outputWriter.Flush();
            }

            if (Data.Count() != processedKeys.Count())
            {
                var missingKeys = string.Join(", ", Data.Keys.Except(processedKeys));
                throw new InvalidOperationException(Resources.FormatError_CommitWhenKeyMissing(missingKeys));
            }
        }

		// Write the contents of newly created config file to given stream
		protected override void GenerateNewConfig(Stream outputStream)
        {
            var outputWriter = new StreamWriter(outputStream);

            foreach (var entry in Data)
            {
                outputWriter.WriteLine(string.Format("{0}={1}", entry.Key, entry.Value));
            }
        }
    }
}
#endif
