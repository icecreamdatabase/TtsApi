using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace TtsApi.Helper
{
    /// <summary>
    /// Static helper class for parsing XML data.
    /// </summary>
    public static class XmlParser
    {
        /// <summary>
        /// Serialize an <see cref="object"/> to an XML <see cref="string"/>.
        /// </summary>
        /// <param name="objectInstance">Instance to serialize.</param>
        /// <returns>Serialized string.</returns>
        public static string XmlSerializeToString(object objectInstance)
        {
            XmlSerializer serializer = new(objectInstance.GetType());
            StringBuilder sb = new();
            XmlWriterSettings xmlSettings = new()
            {
                Indent = true,
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8
            };

            using XmlWriter writer = XmlWriter.Create(sb, xmlSettings);
            serializer.Serialize(
                writer,
                objectInstance,
                new XmlSerializerNamespaces(new[] {XmlQualifiedName.Empty})
            );

            return sb.ToString();
        }

        /// <summary>
        /// Deserialize a <see cref="string"/> to an <see cref="object"/> of type <see cref="T"/>.
        /// </summary>
        /// <param name="objectData">String input to deserialize.</param>
        /// <typeparam name="T">Target <see cref="object"/> type.</typeparam>
        /// <returns>Parsed <see cref="object"/> of type <see cref="T"/>.</returns>
        public static T XmlDeserializeFromString<T>(string objectData)
        {
            XmlSerializer serializer = new(typeof(T));

            using TextReader reader = new StringReader(objectData);
            return (T) serializer.Deserialize(reader);
        }
    }
}
