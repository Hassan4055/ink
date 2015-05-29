﻿using System;
using System.Text;
using System.Collections.Generic;

namespace Inklewriter.Parsed
{
	public class Choice : Parsed.Object
	{
        public string startText { get; protected set; }
        public string choiceOnlyText { get; protected set; }
        public string contentOnlyText { get; protected set; }
		public Path   explicitPath { get; }
        public int    indentationDepth { get; set; } = 1;
        public bool   hasMultiLineContent { get { return this.explicitPath == null; } }

        public Choice (string startText, string choiceOnlyText, string contentOnlyText, Divert divert)
		{
            this.startText = startText;
            this.choiceOnlyText = choiceOnlyText;
            this.contentOnlyText = contentOnlyText;

            if (divert != null) {
                this.explicitPath = divert.target;
            }
		}

        public void AddNestedContent(Parsed.Object obj)
        {
            if (_nestedContent == null) {
                _nestedContent = new List<Parsed.Object> ();
            }

            _nestedContent.Add (obj);
            obj.parent = this;
        }

		public override Runtime.Object GenerateRuntimeObject ()
        {
            // Choice Text
            var choiceTextSB = new StringBuilder ();
            if (startText != null) {
                choiceTextSB.Append (startText);
            }
            if (choiceOnlyText != null) {
                choiceTextSB.Append (choiceOnlyText);
            }

            // Content (Weave style choices)
            var contentTextSB = new StringBuilder ();
            if (startText != null) {
                contentTextSB.Append (startText);
            }
            if (contentOnlyText != null) {
                contentTextSB.Append (contentOnlyText);
            }
            bool hasOwnContent = contentTextSB.Length > 0 || (_nestedContent != null && _nestedContent.Count > 0);

            // Build choice itself
            _runtimeChoice = new Runtime.Choice (choiceTextSB.ToString());

            // Nested content like this:
            // [
            //     choice: -> "c"
            //     (c) = [
            //         weave content
            //     ]
            // ]
            if (hasOwnContent) {

                _weaveContentContainer = new Runtime.Container ();
                _weaveContentContainer.AddContent (new Runtime.Text (contentTextSB.ToString () + "\n"));
                _weaveContentContainer.name = "c";

                if (this.explicitPath != null) {
                    _weaveContentEndDivert = new Runtime.Divert ();
                    _weaveContentContainer.AddContent (_weaveContentEndDivert);
                }

                _weaveOuterContainer = new Runtime.Container ();
                _weaveOuterContainer.AddContent (_runtimeChoice);
                _weaveOuterContainer.AddToNamedContentOnly (_weaveContentContainer);

                if (_nestedContent != null) {
                    foreach(var nestedObj in _nestedContent) {
                        _weaveContentContainer.AddContent(nestedObj.runtimeObject);
                    }
                }

                return _weaveOuterContainer;
            } 

            // Simple/normal choice
            else {
                return _runtimeChoice;
            }
		}

        public override void ResolveReferences(Story context)
		{
			// Weave style choice - target own content container
            if (_weaveContentContainer != null) {
                _runtimeChoice.pathOnChoice = _weaveContentContainer.path;
            }

            // Resolve path that was explicitly specified (either at the end of the weave choice, or just as the normal choice path)
            if (explicitPath != null) {
                Parsed.Object obj = ResolvePath (explicitPath);
                if (obj == null) {
                    Error ("Choice: target not found: '" + explicitPath.ToString () + "'");
                }

                if (_weaveContentEndDivert != null) {
                    _weaveContentEndDivert.targetPath = obj.runtimeObject.path;
                } else {
                    _runtimeChoice.pathOnChoice = obj.runtimeObject.path;
                }
            }

            if (_nestedContent != null) {
                foreach (var obj in _nestedContent) {
                    obj.ResolveReferences (context);
                }
            }
		}

        List<Parsed.Object> _nestedContent;

        Runtime.Choice _runtimeChoice;
        Runtime.Container _weaveContentContainer;
        Runtime.Container _weaveOuterContainer;
        Runtime.Divert _weaveContentEndDivert;
	}

}
