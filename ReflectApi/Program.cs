using System;
using System.IO;
using System.Reflection;
using MFilesAPI;

namespace ReflectedApi
{
    public static class Config
    {
        public static string OutputFile => @"D:\Temp\Reflection.txt";
        public static int MaxDepth => 2;
        public static string Indent => "- ";
    }

    class Program
    {
        static void Main( string[ ] args )
        {
            ProcessType( typeof( IPropertyDef ) );
            Console.WriteLine( "Done" );
        }

        private static void ProcessType(Type type )
        {
            MethodInfo[] methodInfos = type.GetMethods();
            Output( $"{type.Name}" );
            IterateMethods( methodInfos, type );

        }
        /// <summary>
        /// Iterates through the method infos, outputs the current one and then calls itself to iterate
        /// through the interfaces of this the method.
        /// </summary>
        /// <param name="methodInfos"></param>
        /// <param name="parenType"></param>
        /// <param name="depth"></param>
        private static void IterateMethods( MethodInfo[ ] methodInfos, Type parenType, int depth = 1 )
        {
            foreach ( MethodInfo methodInfo in methodInfos )
            {
                Output( $"{methodInfo.Name} {methodInfo.ReturnType.Name}", depth );

                foreach ( Type @interface in methodInfo.ReturnType.GetInterfaces() )
                {
                    if ( !@interface.FullName.StartsWith( "System." ) && @interface != parenType && depth < Config.MaxDepth )
                        IterateMethods( @interface.GetMethods(), @interface, depth + 1 );
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