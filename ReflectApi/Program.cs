using System;
using System.IO;
using System.Reflection;
using MFilesAPI;
using System.Collections.Generic;

namespace ReflectedApi
{
    public static class Config
    {
        public static string OutputFile => @"D:\Temp\Reflection.txt";
        public static int MaxDepth => 4;
        public static string Indent => "- ";
    }

    class Program
    {
        private static Dictionary<string, Type> ProcessedTypes { get; set; }

        static void Main( string[ ] args )
        {
            ProcessedTypes = new Dictionary<string, Type>();

            ProcessType( typeof( Vault ) );
            
            foreach ( KeyValuePair<string, Type> processedType in ProcessedTypes )
            {
                Output( $"{processedType.Value.Name}" );
            }

            Console.WriteLine( "Done" );
        }

        /// <summary>
        /// Processes the given type.
        /// </summary>
        /// <param name="type"></param>
        private static void ProcessType(Type type )
        {
            // Create dictionary for the found types.
            Dictionary<string, Type> foundTypes = new Dictionary<string, Type>();

            // Get interfaces.
            Type[] typeInterfaces = type.GetInterfaces();

            // Iterate through the interfaces.
            foreach ( Type typeInterface in typeInterfaces )
            {
                // Get the methods.
                MethodInfo[] methodInfos = typeInterface.GetMethods();

                // Output what we're processing.
                Output( $"{typeInterface.Name}" );

                // Call the iterate methods method.
                IterateMethods( methodInfos, typeInterface, foundTypes );
            }

            // Add this type to processed types list.
            ProcessedTypes.Add( type.FullName, type );

            // Iterate through found types.
            foreach ( KeyValuePair<string, Type> foundType in foundTypes )
            {
                // If processed types dictionary doesn't (yet) contain this found type, process it.
                if(!ProcessedTypes.ContainsKey(foundType.Key))
                    ProcessType( foundType.Value );
            }
        }

        /// <summary>
        /// Iterates through the method infos, outputs the current one and then calls itself to iterate
        /// through the interfaces of this the method.
        /// </summary>
        /// <param name="methodInfos"></param>
        /// <param name="parenType"></param>
        /// <param name="depth"></param>
        private static void IterateMethods( MethodInfo[ ] methodInfos, Type parenType, Dictionary<string, Type> foundTypes, int depth = 1 )
        {
            // Safety mechanism.
            // If requested depth is deeper than max depth, return.
            if( depth > Config.MaxDepth )
                return;

            // Iterate through the methods infos
            foreach ( MethodInfo methodInfo in methodInfos )
            {
                // If the name starts with get_ or set_ or return type full name begins with MFilesAPI.
                // there is a method that we're interested to output, otherwise it is likely a system
                // method like .ToString or so.
                if ( methodInfo.Name.StartsWith( "get_" ) || methodInfo.Name.StartsWith( "set_" ) ||
                     methodInfo.ReturnType.FullName.StartsWith( "MFilesAPI." ) )
                    Output( $"{parenType.FullName} {methodInfo.Name} {methodInfo.ReturnType.Name}", depth );

                // If the return type full name starts with MFilesAPI. and it is not same as parent type,
                // and not yet in the found types dictionary. it is a type that we're interested of.
                if ( methodInfo.ReturnType.FullName.StartsWith( "MFilesAPI." ) &&
                     methodInfo.ReturnType != parenType &&
                     !foundTypes.ContainsKey( methodInfo.ReturnType.FullName ) )
                {
                    // Add it to found types dictionary, and call the processing method.
                    foundTypes.Add( methodInfo.ReturnType.FullName, methodInfo.ReturnType );
                    IterateMethods( methodInfo.ReturnType.GetMethods(), methodInfo.ReturnType, foundTypes, depth + 1 );
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

            output = $"{output}{text}";

            using ( StreamWriter file = new StreamWriter( Config.OutputFile, true ) )
            {
                file.WriteLine( output );
            }
        }
    }
}