using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace _Project.Runtime.Core.Extensions
{
  public static class CloneExtensions
  {
    public static IList<T> Clone<T>(this IList<T> listToClone) where T: ICloneable
    {
      return listToClone.Select(item => (T)item.Clone()).ToList();
    }
    
    public static T Clone<T>(this T source)
    {
      if (!typeof(T).IsSerializable)
      {
        throw new ArgumentException("The type must be serializable.", nameof(source));
      }

      // Don't serialize a null object, simply return the default for that object
      if (ReferenceEquals(source, null)) return default;

      using Stream stream = new MemoryStream();
      IFormatter formatter = new BinaryFormatter();
      formatter.Serialize(stream, source);
      stream.Seek(0, SeekOrigin.Begin);
      return (T)formatter.Deserialize(stream);
    }
  }
}
