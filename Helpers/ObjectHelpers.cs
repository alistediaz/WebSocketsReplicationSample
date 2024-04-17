using System.Xml.Serialization;

namespace WebSocketsSample.Helpers
{
    static class ObjectHelpers
    {
        public static string SerializeObject<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new(toSerialize.GetType());

            using (StringWriter textWriter = new())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        public static bool EqualTo(this object obj, object toCompare)
        {
            if (obj.SerializeObject() == toCompare.SerializeObject())
                return true;
            else
                return false;
        }

        public static bool IsBlank<T>(this T obj) where T : new()
        {
            T blank = new();
            T newObj = ((T)obj);

            if (newObj.SerializeObject() == blank.SerializeObject())
                return true;
            else
                return false;
        }

    }
}
