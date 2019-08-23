using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ITLec.ConsoleApp.BRE.LINQExp
{
    class Program
    {
        public static void Main()
        {
            var user2 = new User
            {
                Age = 33,
                Name = "Mohamed",
                Address = new Address()
                {
                    StreetName = "Rasheed"
                ,
                    MoreDetailedAdress = new Address() { StreetName = "Mariam", PostalCode = 5 }
                }
            };
            var singleRule = new Rule("", "IsActiveUser", "Rasheed");
            //  var singleRule = new Rule("Address", "IsValid", "");
            //  var singleRule = new Rule("Address", "IsValid", "true");
            //     var singleRule = new Rule("", "HasVal", "");
            //      var rule = new Rule("Address.MoreDetailedAdress.PostalCode", "GreaterThan", "3");
            //   var rule = new Rule("Age", "Equal", "12");

            //   var rule = new Rule("Address.MoreDetailedAdress.StreetName", "Contains","M");
            Func<User, bool> compiledRule = CompileRule<User>(singleRule);
            bool ii = compiledRule(user2);

            List<Rule> rules = new List<Rule> {
    new Rule("Address", "IsValid", "true"),
    new Rule("Address.MoreDetailedAdress.PostalCode", "GreaterThan", "3"),
    new Rule("Address.MoreDetailedAdress.StreetName", "Contains","Z"),
    new Rule("", "HasVal", ""),
    new Rule("", "IsActiveUser", "Rasheed"),
    new Rule("Name", "NotEqual", "Rasheed"),

            };


            var compiledRules = rules.Select(r => CompileRule<User>(r)).ToList();
            var totalResult = compiledRules.All(rule => rule(user2));


        }



        public static Func<T, bool> CompileRule<T>(Rule r)
        {
            var paramUser = Expression.Parameter(typeof(T));
            Expression expr = BuildExpr<T>(r, paramUser);
            // build a lambda function User->bool and compile it
            return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
        }

        public class Rule
        {
            public string MemberName
            {
                get;
                set;
            }

            public string Operator
            {
                get;
                set;
            }

            public object TargetValue
            {
                get;
                set;
            }

            public Rule(string MemberName, string Operator, object TargetValue)
            {
                this.MemberName = MemberName;
                this.Operator = Operator;
                this.TargetValue = TargetValue;
            }
        }



        static Expression BuildExpr<T>(Rule r, ParameterExpression param)
        {
            //   MemberExpression.
            Expression left = param;// Expression.Constant(param);
            Type tProp = typeof(T);
            if (r.MemberName.Length > 0)
            {
                string[] strs = r.MemberName.Split('.');
                left = MemberExpression.Property(param, strs[0]);


                for (int i = 1; i < strs.Length; i++)
                {
                    left = MemberExpression.Property(left, strs[i]);
                }
                var propInfo = typeof(T).GetProperty(strs[0]);


                for (int i = 1; i < strs.Length; i++)
                {
                    propInfo = (propInfo.PropertyType).GetProperty(strs[i]);
                }
                tProp = propInfo.PropertyType;
            }
            ExpressionType tBinary;
            // is the operator a known .NET operator?
            if (ExpressionType.TryParse(r.Operator, out tBinary))
            {
                var right = Expression.Constant(Convert.ChangeType(r.TargetValue, tProp));

                // use a binary operation, e.g. 'Equal' -> 'u.Age == 15'
                return Expression.MakeBinary(tBinary, left, right);
            }
            else
            {
                System.Reflection.MethodInfo method = null;
                Expression right = null;
                if (r.TargetValue != null && !string.IsNullOrEmpty(r.TargetValue.ToString()))
                {
                    method = tProp.GetMethod(r.Operator, new[] { r.TargetValue.GetType() });
                }
                else
                {

                    //  method= tProp.GetMethod(r.Operator);

                    foreach (var _method in tProp.GetMethods())
                    {
                        if (_method.Name == r.Operator && _method.GetParameters().Length == 0)
                        {
                            try
                            {
                                method = _method;
                                break;
                            }
                            catch
                            {

                            }
                        }
                    }
                }


                if (method == null)
                {
                    foreach (var _method in tProp.GetMethods())
                    {
                        if (_method.Name == r.Operator && _method.GetParameters().Length == 1)
                        {
                            var _methodParam = _method.GetParameters()[0];
                            try
                            {
                                Convert.ChangeType(r.TargetValue, _methodParam.ParameterType);
                                method = _method;
                                break;
                            }
                            catch
                            {

                            }
                        }
                    }
                }



                if (r.TargetValue != null && method.GetParameters().Length > 0)
                {
                    var tParam = method.GetParameters()[0].ParameterType;
                    right = Expression.Constant(Convert.ChangeType(r.TargetValue, tParam));
                }
                else
                {
                    right = Expression.Constant("");
                    return Expression.Call(left, method);
                }

                // use a method call, e.g. 'Contains' -> 'u.Tags.Contains(some_tag)'
                return Expression.Call(left, method, right);
            }
        }


        public class User
        {
            public bool IsActiveUser(string password)
            {
                return "Rasheed" == password && this.Address.StreetName == password;
            }
            public bool HasVal()
            {
                return true;
            }
            public Address Address
            {
                get; set;
            }
            public int Age
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }
        }




        public class Address
        {
            public string StreetName { get; set; }

            public bool IsValid()
            {
                return false;
            }
            public bool IsValid(bool isInternational)
            {

                return isInternational;
            }

            public int PostalCode { get; set; }

            public Address MoreDetailedAdress { get; set; }
        }
    }
}
