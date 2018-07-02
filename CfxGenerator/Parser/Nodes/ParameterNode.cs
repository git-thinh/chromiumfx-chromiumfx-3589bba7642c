// Copyright (c) 2014-2017 Wolfgang Borgsmüller
// All rights reserved.
// 
// This software may be modified and distributed under the terms
// of the BSD license. See the License.txt file for details.

using System;

namespace Parser {

    [Serializable()]
    public class ParameterNode {
        public TypeNode ParameterType = new TypeNode();
        public string Var;
        public bool IsConst;

        public override string ToString() {
            return string.Format("{0} {1}{2}", ParameterType, Var, IsConst ? " (const)" : "");
        }
    }
}
