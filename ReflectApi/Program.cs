using System;
using System.IO;
using System.Reflection;
using MFilesAPI;
using System.Collections.Generic;

namespace ReflectApi
{
    public static class Config
    {
        public static string OutputFile => @"D:\Temp\Reflection.txt";
        public static string WrapeprFile => @"D:\Temp\ApiWrapper.cs";
        public static int MaxDepth => 1;
        public static string Indent => "|";
    }

    class Program
    {
        private static Dictionary< string, Type > ProcessedTypes { get; set; }
        public static int TotalCount { get; set; }

        static void Main( string[] args )
        {
            // Delete the previous output files.
            File.Delete( Config.WrapeprFile );
            File.Delete( Config.OutputFile );

            // Set total count to 0.
            TotalCount = 0;

            // Create dictionary of the processed types, used to prevent processing
            // same type multiple times.
            ProcessedTypes = new Dictionary< string, Type >();

            // Call process type from Vault object's point of view.
            ProcessType( typeof( Vault ) );

            // Finally output all processed type names.
            foreach ( KeyValuePair< string, Type > processedType in ProcessedTypes )
            {
                Output( $"{processedType.Value.Name}" );
            }

            Console.WriteLine("Processed the interfaces, generating the wrapper.");

            GenerateWrapper();

            Console.WriteLine( "Done" );
        }

        private static void GenerateWrapper()
        {
            string indent = "    ";
            using ( StreamWriter file = new StreamWriter( Config.WrapeprFile, true ) )
            {
                file.WriteLine( "using System;" );
                file.WriteLine( "using MFilesAPI;" );
                file.WriteLine( "" );
                file.WriteLine( "namespace xWrappersNew" );
                file.WriteLine( "{" );
                file.WriteLine( $"{indent}public class WrappersNew" );
                file.WriteLine( $"{indent}{{" );

                foreach ( KeyValuePair< string, Type > processedType in ProcessedTypes )
                {
                    file.WriteLine($"{indent}{indent}public class x{processedType.Value.Name}");
                    file.WriteLine($"{indent}{indent}{{");

                    foreach ( Type typeInterface in processedType.Value.GetInterfaces() )
                    {
                        foreach ( MethodInfo methodInfo in typeInterface.GetMethods() )
                        {
                            if(methodInfo.ReturnType == processedType.Value)
                                continue;

                            if ( methodInfo.ReturnType.FullName.StartsWith( "MFilesAPI." ) )
                            {
                                file.WriteLine( $"{indent}{indent}{indent}public x{methodInfo.ReturnType.Name} {GetMethodName(methodInfo)} {{ get; set; }}" );
                            }
                            else if ( methodInfo.ReturnType.FullName.StartsWith( "System." ) )
                            {
                                if(methodInfo.ReturnType.Name.ToLower() != "void" )
                                    file.WriteLine( $"{indent}{indent}{indent}public {methodInfo.ReturnType.Name} {GetMethodName(methodInfo)} {{ get; set; }}" );
                            }
                        }
                        
                        file.WriteLine( $"{indent}{indent}{indent}public x{processedType.Value.Name}({processedType.Value.Name} apiObj)" );
                        file.WriteLine($"{indent}{indent}{indent}{{");

                        foreach (MethodInfo methodInfo in typeInterface.GetMethods())
                        {
                            if ( methodInfo.ReturnType.Name.ToLower() != "void" && GetMethodName(methodInfo) != processedType.Value.Name && methodInfo.Name.StartsWith("get_"))
                            {
                                if(methodInfo.ReturnType.FullName.StartsWith("MFilesAPI."))
                                    file.WriteLine($"{indent}{indent}{indent}{indent}this.{GetMethodName(methodInfo)} = new x{GetMethodName(methodInfo)}( apiObj.{GetMethodNameForConstructor(methodInfo)} );");
                                else
                                    file.WriteLine($"{indent}{indent}{indent}{indent}this.{GetMethodName(methodInfo)} = apiObj.{GetMethodNameForConstructor(methodInfo)};");
                            }
                        }       
                        file.WriteLine($"{indent}{indent}{indent}}}");
                    }
                    file.WriteLine($"{indent}{indent}}}");
                }
                file.WriteLine( $"{indent}}}" );
                file.WriteLine( "}" );
            }
        }

        /// <summary>
        /// Parses the method name from method info, removes get_ and set_ prefixes.
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="removeVaultPrefix"></param>
        /// <returns></returns>
        private static string GetMethodName( MethodInfo methodInfo )
        {
            string methodname = "";

            if ( methodInfo.ReturnType.FullName.StartsWith( "MFilesAPI." ) )
                methodname = methodInfo.ReturnType.Name;
            else if ( methodInfo.ReturnType.FullName.StartsWith( "System." ) )
                methodname = methodInfo.Name;

            if ( methodname.StartsWith( "get_" ) || methodname.StartsWith( "set_" ) )
                methodname = methodname.Substring( 4, methodname.Length - 4 );

            return methodname;
        }

        private static string GetMethodNameForConstructor(MethodInfo methodInfo)
        {
            string methodname = "";

            if (methodInfo.ReturnType.FullName.StartsWith("MFilesAPI."))
                methodname = methodInfo.ReturnType.Name;
            else if (methodInfo.ReturnType.FullName.StartsWith("System."))
                methodname = methodInfo.Name;

            if (methodname.StartsWith("get_") || methodname.StartsWith("set_"))
                methodname = methodname.Substring(4, methodname.Length - 4);

            if (methodname.StartsWith("Vault"))
                methodname = methodname.Substring(5, methodname.Length - 5);

            return methodname;
        }

        /// <summary>
        /// Processes the given type.
        /// </summary>
        /// <param name="type"></param>
        private static void ProcessType( Type type )
        {
            // Create dictionary for the found types.
            Dictionary< string, Type > foundTypes = new Dictionary< string, Type >();

            // Get interfaces.
            Type[] typeInterfaces = type.GetInterfaces();

            // Iterate through the interfaces.
            foreach ( Type typeInterface in typeInterfaces )
            {
                // Get the methods.
                MethodInfo[] methodInfos = typeInterface.GetMethods();

                // Output what we're processing.
                Output( $"{type.Name} {typeInterface.Name}" );

                // Call the iterate methods method.
                IterateMethods( methodInfos, typeInterface, foundTypes );
            }

            // Add this type to processed types list.
            ProcessedTypes.Add( type.FullName, type );

            // Iterate through found types.
            foreach ( KeyValuePair< string, Type > foundType in foundTypes )
            {
                // If processed types dictionary doesn't (yet) contain this found type, process it.
                if ( !ProcessedTypes.ContainsKey( foundType.Key ) )
                    ProcessType( foundType.Value );
            }
        }

        /// <summary>
        /// Iterates through the method infos, outputs the current one and then calls itself to iterate
        /// through the interfaces of this the method.
        /// </summary>
        /// <param name="methodInfos"></param>
        /// <param name="parenType"></param>
        /// <param name="foundTypes"></param>
        /// <param name="depth"></param>
        private static void IterateMethods( MethodInfo[] methodInfos, Type parenType, Dictionary< string, Type > foundTypes, int depth = 1 )
        {
            // Report progress to command line.
            TotalCount++;
            Console.Write( $"\r{TotalCount}" );

            // Safety mechanism.
            // If requested depth is deeper than max depth, return.
            if ( depth > Config.MaxDepth )
                return;

            // Iterate through the methods infos
            foreach ( MethodInfo methodInfo in methodInfos )
            {
                // If the name starts with get_ or set_ or return type full name begins with MFilesAPI.
                // there is a method that we're interested to output, otherwise it is likely a system
                // method like .ToString or so.
                if ( methodInfo.Name.StartsWith( "get_" ) || methodInfo.Name.StartsWith( "set_" ) ||
                     methodInfo.ReturnType.FullName.StartsWith( "MFilesAPI." ) )
                    Output( $"{methodInfo.Name} {methodInfo.ReturnType.Name}", depth );

                // If the return type full name starts with MFilesAPI. and it is not same as parent type,
                // and not yet in the found types dictionary. it is a type that we're interested of.
                if ( methodInfo.ReturnType.FullName.StartsWith( "MFilesAPI." ) &&
                     methodInfo.ReturnType != parenType &&
                     !foundTypes.ContainsKey( methodInfo.ReturnType.FullName ) )
                {
                    // Add it to found types dictionary, and call the processing method.
                    foundTypes.Add( methodInfo.ReturnType.FullName, methodInfo.ReturnType );
                    // Or don't call... too many nested repeated items...
                    //IterateMethods( methodInfo.ReturnType.GetMethods(), methodInfo.ReturnType, foundTypes, depth + 1 );
                }
                // If the method doesn't start with System. it is something that we might be interested.
                // Essentially this is a sanity check from earlier iterations where attempting to resolve
                // all interesting methods.
                else if ( !methodInfo.ReturnType.FullName.StartsWith( "System." ) )
                {
                    foreach ( Type @interface in methodInfo.ReturnType.GetInterfaces() )
                    {
                        if ( !@interface.FullName.StartsWith( "System." ) && @interface != parenType )
                            IterateMethods( @interface.GetMethods(), @interface, foundTypes, depth + 1 );
                    }
                }
            }
        }

        /// <summary>
        /// Outputs the given text into the configured file.
        /// Indents the depth with configured string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="depth"></param>
        private static void Output( string text, int depth = 0 )
        {
            string output = "";

            for ( int i = 0; i < depth; i++ )
            {
                output = $"{Config.Indent}{output}";
            }

            output = $"{output} {text}";

            using ( StreamWriter file = new StreamWriter( Config.OutputFile, true ) )
            {
                file.WriteLine( output );
            }
        }
    }
}