using System;
using System.Reflection;
using System.Text;
using log4net;
using NHibernate.Properties;
using NHibernate.Type;

namespace NHibernate.Util
{
	/// <summary>
	/// Helper class for Reflection related code.
	/// </summary>
	public sealed class ReflectHelper
	{
		private static readonly ILog log = LogManager.GetLogger(typeof (ReflectHelper));

		public static BindingFlags AnyVisibilityInstance = BindingFlags.Instance | BindingFlags.Public |
		                                                   BindingFlags.NonPublic;

		private ReflectHelper()
		{
			// not creatable
		}

		private static System.Type[] NoClasses = System.Type.EmptyTypes;

		private static readonly MethodInfo Exception_InternalPreserveStackTrace =
			typeof (Exception).GetMethod("InternalPreserveStackTrace", BindingFlags.Instance | BindingFlags.NonPublic);

		/// <summary>
		/// Determine if the specified <see cref="System.Type"/> overrides the
		/// implementation of Equals from <see cref="Object"/>
		/// </summary>
		/// <param name="clazz">The <see cref="System.Type"/> to reflect.</param>
		/// <returns><see langword="true" /> if any type in the hierarchy overrides Equals(object).</returns>
		public static bool OverridesEquals(System.Type clazz)
		{
			try
			{
				MethodInfo equals = clazz.GetMethod("Equals", new System.Type[] {typeof (object)});
				if (equals == null)
				{
					return false;
				}
				else
				{
					// make sure that the DeclaringType is not System.Object - if that is the
					// declaring type then there is no override.
					return !equals.DeclaringType.Equals(typeof (object));
				}
			}
			catch (AmbiguousMatchException)
			{
				// an ambigious match means that there is an override and it
				// can't determine which one to use.
				return true;
			}
		}

		/// <summary>
		/// Determine if the specified <see cref="System.Type"/> overrides the
		/// implementation of GetHashCode from <see cref="Object"/>
		/// </summary>
		/// <param name="clazz">The <see cref="System.Type"/> to reflect.</param>
		/// <returns><see langword="true" /> if any type in the hierarchy overrides GetHashCode().</returns>
		public static bool OverridesGetHashCode(System.Type clazz)
		{
			try
			{
				MethodInfo getHashCode = clazz.GetMethod("GetHashCode", new System.Type[0]);
				if (getHashCode == null)
				{
					return false;
				}
				else
				{
					// make sure that the DeclaringType is not System.Object - if that is the
					// declaring type then there is no override.
					return !getHashCode.DeclaringType.Equals(typeof (object));
				}
			}
			catch (AmbiguousMatchException)
			{
				// an ambiguous match means that there is an override and it
				// can't determine which one to use.
				return true;
			}
		}

		/// <summary>
		/// Finds the <see cref="IGetter"/> for the property in the <see cref="System.Type"/>.
		/// </summary>
		/// <param name="theClass">The <see cref="System.Type"/> to find the property in.</param>
		/// <param name="propertyName">The name of the Property to find.</param>
		/// <param name="propertyAccessorName">The name of the property access strategy.</param>
		/// <returns>The <see cref="IGetter"/> to get the value of the Property.</returns>
		/// <remarks>
		/// This one takes a propertyAccessor name as we might know the correct strategy by now so we avoid Exceptions which are costly
		/// </remarks>
		public static IGetter GetGetter(System.Type theClass, string propertyName, string propertyAccessorName)
		{
			return PropertyAccessorFactory
				.GetPropertyAccessor(propertyAccessorName)
				.GetGetter(theClass, propertyName);
		}

		//TODO: add a method in here ReflectedPropertyClass and replace most calls to GetGetter
		// with calls to it

		/// <summary>
		/// Get the NHibernate <see cref="IType" /> for the named property of the <see cref="System.Type"/>.
		/// </summary>
		/// <param name="theClass">The <see cref="System.Type"/> to find the Property in.</param>
		/// <param name="name">The name of the property/field to find in the class.</param>
		/// <param name="access">The name of the property accessor for the property.</param>
		/// <returns>
		/// The NHibernate <see cref="IType"/> for the named property.
		/// </returns>
		public static IType ReflectedPropertyType(System.Type theClass, string name, string access)
		{
			System.Type propertyClass = ReflectedPropertyClass(theClass, name, access);

			System.Type heuristicClass = propertyClass;

			if (propertyClass.IsGenericType
			    && propertyClass.GetGenericTypeDefinition().Equals(typeof (Nullable<>)))
			{
				heuristicClass = propertyClass.GetGenericArguments()[0];
			}

			return TypeFactory.HeuristicType(heuristicClass.AssemblyQualifiedName);
		}

		/// <summary>
		/// Get the <see cref="System.Type" /> for the named property of a type.
		/// </summary>
		/// <param name="theClass">The <see cref="System.Type"/> to find the property in.</param>
		/// <param name="name">The name of the property/field to find in the class.</param>
		/// <param name="access">The name of the property accessor for the property.</param>
		/// <returns>The <see cref="System.Type" /> for the named property.</returns>
		public static System.Type ReflectedPropertyClass(System.Type theClass, string name, string access)
		{
			return GetGetter(theClass, name, access).ReturnType;
		}

		/// <summary>
		/// Get the <see cref="System.Type" /> for the named property of a type.
		/// </summary>
		/// <param name="className">The FullName to find the property in.</param>
		/// <param name="name">The name of the property/field to find in the class.</param>
		/// <param name="accessorName">The name of the property accessor for the property.</param>
		/// <returns>The <see cref="System.Type" /> for the named property.</returns>
		public static System.Type ReflectedPropertyClass(string className, string name, string accessorName)
		{
			try
			{
				System.Type clazz = ClassForName(className);
				return GetGetter(clazz, name, accessorName).ReturnType;
			}
			catch (Exception cnfe)
			{
				throw new MappingException(string.Format("class {0} not found while looking for property: {1}", className, name),
				                           cnfe);
			}
		}

		/// <summary>
		/// Returns a reference to the Type.
		/// </summary>
		/// <param name="name">The name of the class or a fully qualified name.</param>
		/// <returns>The Type for the Class.</returns>
		public static System.Type ClassForName(string name)
		{
			AssemblyQualifiedTypeName parsedName = TypeNameParser.Parse(name);
			System.Type result = TypeFromAssembly(parsedName, true);
			return result;
		}

		/// <summary>
		/// Load a System.Type given is't name.
		/// </summary>
		/// <param name="classFullName">The class FullName or AssemblyQualifiedName</param>
		/// <returns>The System.Type</returns>
		/// <remarks>
		/// If the <paramref name="classFullName"/> don't represent an <see cref="System.Type.AssemblyQualifiedName"/>
		/// the method try to find the System.Type scanning all Assemblies of the <see cref="AppDomain.CurrentDomain"/>.
		/// </remarks>
		/// <exception cref="TypeLoadException">If no System.Type was found for <paramref name="classFullName"/>.</exception>
		public static System.Type ClassForFullName(string classFullName)
		{
			System.Type result = null;
			AssemblyQualifiedTypeName parsedName = TypeNameParser.Parse(classFullName);
			if (!string.IsNullOrEmpty(parsedName.Assembly))
			{
				result = TypeFromAssembly(parsedName, false);
			}
			else
			{
				if (classFullName != null && classFullName.Length > 0)
				{
					Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();
					foreach (Assembly a in ass)
					{
						result = a.GetType(classFullName, false, false);
						if (result != null)
							break; //<<<<<================
					}
				}
			}
			if (result == null)
			{
				string message = "Could not load type " + classFullName +
				                 ". Possible cause: the assembly was not loaded or not specified.";
				throw new TypeLoadException(message);
			}

			return result;
		}

		public static System.Type TypeFromAssembly(string type, string assembly, bool throwIfError)
		{
			return TypeFromAssembly(new AssemblyQualifiedTypeName(type, assembly), throwIfError);
		}

		/// <summary>
		/// Returns a <see cref="System.Type"/> from an already loaded Assembly or an
		/// Assembly that is loaded with a partial name.
		/// </summary>
		/// <param name="name">An <see cref="AssemblyQualifiedTypeName" />.</param>
		/// <param name="throwOnError"><see langword="true" /> if an exception should be thrown
		/// in case of an error, <see langword="false" /> otherwise.</param>
		/// <returns>
		/// A <see cref="System.Type"/> object that represents the specified type,
		/// or <see langword="null" /> if the type cannot be loaded.
		/// </returns>
		/// <remarks>
		/// Attempts to get a reference to the type from an already loaded assembly.  If the 
		/// type cannot be found then the assembly is loaded using
		/// <see cref="Assembly.Load(string)" />.
		/// </remarks>
		public static System.Type TypeFromAssembly(AssemblyQualifiedTypeName name, bool throwOnError)
		{
			try
			{
				// Try to get the type from an already loaded assembly
				System.Type type = System.Type.GetType(name.ToString());

				if (type != null)
				{
					return type;
				}

				if (name.Assembly == null)
				{
					// No assembly was specified for the type, so just fail
					string message = "Could not load type " + name + ". Possible cause: no assembly name specified.";
					log.Warn(message);
					if (throwOnError) throw new TypeLoadException(message);
					return null;
				}

				Assembly assembly = Assembly.Load(name.Assembly);

				if (assembly == null)
				{
					log.Warn("Could not load type " + name + ". Possible cause: incorrect assembly name specified.");
					return null;
				}

				type = assembly.GetType(name.Type, throwOnError);

				if (type == null)
				{
					log.Warn("Could not load type " + name + ".");
					return null;
				}

				return type;
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
				{
					log.Error("Could not load type " + name + ".", e);
				}
				if (throwOnError) throw;
				return null;
			}
		}

		public static bool TryLoadAssembly(string assemblyName)
		{
			if (string.IsNullOrEmpty(assemblyName))
				return false;

			bool result = true;
			try
			{
				Assembly.Load(assemblyName);
			}
			catch (Exception)
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Returns the value of the static field <paramref name="fieldName"/> of <paramref name="type"/>.
		/// </summary>
		/// <param name="type">The <see cref="System.Type"/> .</param>
		/// <param name="fieldName">The name of the field in the <paramref name="type"/>.</param>
		/// <returns>The value contained in the field, or <see langword="null" /> if the type or the field does not exist.</returns>
		public static object GetConstantValue(System.Type type, string fieldName)
		{
			try
			{
				FieldInfo field = type.GetField(fieldName);
				if (field == null)
				{
					return null;
				}
				return field.GetValue(null);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the default no arg constructor for the <see cref="System.Type"/>.
		/// </summary>
		/// <param name="type">The <see cref="System.Type"/> to find the constructor for.</param>
		/// <returns>
		/// The <see cref="ConstructorInfo"/> for the no argument constructor, or <see langword="null" /> if the
		/// <c>type</c> is an abstract class.
		/// </returns>
		/// <exception cref="InstantiationException">
		/// Thrown when there is a problem calling the method GetConstructor on <see cref="System.Type"/>.
		/// </exception>
		public static ConstructorInfo GetDefaultConstructor(System.Type type)
		{
			if (IsAbstractClass(type))
				return null;

			try
			{
				ConstructorInfo constructor =
					type.GetConstructor(AnyVisibilityInstance, null, CallingConventions.HasThis, NoClasses, null);
				return constructor;
			}
			catch (Exception e)
			{
				throw new InstantiationException("A default (no-arg) constructor could not be found for: ", e, type);
			}
		}

		/// <summary>
		/// Finds the constructor that takes the parameters.
		/// </summary>
		/// <param name="type">The <see cref="System.Type"/> to find the constructor in.</param>
		/// <param name="types">The <see cref="IType"/> objects to use to find the appropriate constructor.</param>
		/// <returns>
		/// An <see cref="ConstructorInfo"/> that can be used to create the type with
		/// the specified parameters.
		/// </returns>
		/// <exception cref="InstantiationException">
		/// Thrown when no constructor with the correct signature can be found.
		/// </exception>
		public static ConstructorInfo GetConstructor(System.Type type, IType[] types)
		{
			ConstructorInfo[] candidates = type.GetConstructors(AnyVisibilityInstance);

			foreach (ConstructorInfo constructor in candidates)
			{
				ParameterInfo[] parameters = constructor.GetParameters();

				if (parameters.Length == types.Length)
				{
					bool found = true;

					for (int j = 0; j < parameters.Length; j++)
					{
						bool ok = parameters[j].ParameterType.IsAssignableFrom(
							types[j].ReturnedClass);

						if (!ok)
						{
							found = false;
							break;
						}
					}

					if (found)
					{
						return constructor;
					}
				}
			}

			throw new InstantiationException(FormatConstructorNotFoundMessage(types), null, type);
		}

		private static string FormatConstructorNotFoundMessage(IType[] types)
		{
			StringBuilder result = new StringBuilder("no constructor compatible with (");
			bool first = true;
			foreach (IType type in types)
			{
				if (!first)
				{
					result.Append(", ");
				}
				first = false;
				result.Append(type.ReturnedClass);
			}
			result.Append(") found in class: ");
			return result.ToString();
		}

		/// <summary>
		/// Determines if the <see cref="System.Type"/> is a non creatable class.
		/// </summary>
		/// <param name="type">The <see cref="System.Type"/> to check.</param>
		/// <returns><see langword="true" /> if the <see cref="System.Type"/> is an Abstract Class or an Interface.</returns>
		public static bool IsAbstractClass(System.Type type)
		{
			return (type.IsAbstract || type.IsInterface);
		}

		public static bool IsFinalClass(System.Type type)
		{
			return type.IsSealed;
		}

		/// <summary>
		/// Unwraps the supplied <see cref="System.Reflection.TargetInvocationException"/> 
		/// and returns the inner exception preserving the stack trace.
		/// </summary>
		/// <param name="ex">
		/// The <see cref="System.Reflection.TargetInvocationException"/> to unwrap.
		/// </param>
		/// <returns>The unwrapped exception.</returns>
		public static Exception UnwrapTargetInvocationException(TargetInvocationException ex)
		{
			Exception_InternalPreserveStackTrace.Invoke(ex.InnerException, new Object[] {});
			return ex.InnerException;
		}
	}
}