﻿namespace GraphQLCore.Execution
{
    using Language.AST;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using Type;
    using Utils;

    public class ExecutionContext : IDisposable
    {
        internal GraphQLSchema GraphQLSchema;
        private GraphQLDocument ast;
        private FieldCollector fieldCollector;
        private Dictionary<string, GraphQLFragmentDefinition> fragments;
        private GraphQLOperationDefinition operation;
        private VariableResolver operationVariableResolver;
        private dynamic variables;

        public ExecutionContext(GraphQLSchema graphQLSchema, GraphQLDocument ast)
        {
            this.GraphQLSchema = graphQLSchema;
            this.ast = ast;
            this.fragments = new Dictionary<string, GraphQLFragmentDefinition>();
            this.fieldCollector = new FieldCollector(this.fragments, this);
            this.variables = new ExpandoObject();
        }

        public ExecutionContext(GraphQLSchema graphQLSchema, GraphQLDocument ast, dynamic variables)
            : this(graphQLSchema, ast)
        {
            this.variables = variables;
        }

        public void Dispose()
        {
        }

        public dynamic Execute()
        {
            foreach (var definition in this.ast.Definitions)
                this.ResolveDefinition(definition);

            if (this.operation == null)
                throw new Exception("Must provide an operation.");

            this.operationVariableResolver = new VariableResolver(
                this.variables, this.GraphQLSchema.TypeTranslator, this.operation.VariableDefinitions);
            var type = this.GetOperationRootType();

            return ComposeResultForType(type, this.operation.SelectionSet);
        }

        public object[] FetchArgumentValues(LambdaExpression expression, IList<GraphQLArgument> arguments)
        {
            return ReflectionUtilities.GetParameters(expression)
                .Select(e => ReflectionUtilities.ChangeValueType(GetArgumentValue(arguments, e.Name), e.Type))
                .ToArray();
        }

        public object GetArgumentValue(IEnumerable<GraphQLArgument> arguments, string argumentName)
        {
            var value = arguments.SingleOrDefault(e => e.Name.Value == argumentName).Value;

            return this.GetValue(value);
        }

        public IEnumerable GetListValue(Language.AST.GraphQLValue value)
        {
            IList output = new List<object>();
            var list = ((GraphQLValue<IEnumerable<GraphQLValue>>)value).Value;

            foreach (var item in list)
                output.Add(GetValue(item));

            return output;
        }

        public object GetValue(GraphQLValue value)
        {
            var literalValue = this.GraphQLSchema.TypeTranslator.GetLiteralValue(value);

            if (literalValue != null)
                return literalValue;

            switch (value.Kind)
            {
                case ASTNodeKind.ListValue: return GetListValue(value);
                case ASTNodeKind.Variable: return this.operationVariableResolver.GetValue((GraphQLVariable)value);
            }

            throw new NotImplementedException();
        }

        public object InvokeWithArguments(IList<Language.AST.GraphQLArgument> arguments, LambdaExpression expression)
        {
            var argumentValues = FetchArgumentValues(expression, arguments);

            return expression.Compile().DynamicInvoke(argumentValues);
        }

        internal dynamic ComposeResultForType(GraphQLObjectType type, GraphQLSelectionSet selectionSet, object parentObject = null)
        {
            var scope = CreateScope(type, parentObject);
            return GetResultFromScope(type, selectionSet, scope);
        }

        internal FieldScope CreateScope(GraphQLObjectType type, object parentObject)
        {
            return new FieldScope(this, type, parentObject, this.operationVariableResolver);
        }

        internal dynamic GetResultFromScope(GraphQLObjectType type, GraphQLSelectionSet selectionSet, FieldScope scope)
        {
            var fields = this.fieldCollector.CollectFields(type, selectionSet);
            return scope.GetObject(fields);
        }

        private GraphQLObjectType GetOperationRootType()
        {
            switch (this.operation.Operation)
            {
                case OperationType.Query: return this.GraphQLSchema.QueryType;
                default: throw new Exception("Can only execute queries");
            }
        }

        private void ResolveDefinition(ASTNode definition)
        {
            switch (definition.Kind)
            {
                case ASTNodeKind.OperationDefinition:
                    this.ResolveOperationDefinition(definition as GraphQLOperationDefinition); break;
                case ASTNodeKind.FragmentDefinition:
                    this.ResolveFragmentDefinition(definition as GraphQLFragmentDefinition); break;
                default: throw new Exception($"GraphQL cannot execute a request containing a {definition.Kind}.");
            }
        }

        private void ResolveFragmentDefinition(GraphQLFragmentDefinition graphQLFragmentDefinition)
        {
            this.fragments.Add(graphQLFragmentDefinition.Name.Value, graphQLFragmentDefinition);
        }

        private void ResolveOperationDefinition(GraphQLOperationDefinition graphQLOperationDefinition)
        {
            if (this.operation != null)
                throw new Exception("Must provide operation name if query contains multiple operations.");

            if (this.operation == null)
                this.operation = graphQLOperationDefinition;
        }
    }
}