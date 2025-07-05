using UnityEditor;
using UnityEngine;

namespace BatchRenderingTool.RecorderEditors
{
    /// <summary>
    /// Base class for all recorder settings editors
    /// Follows Unity Recorder's standard UI pattern
    /// </summary>
    public abstract class RecorderSettingsEditorBase
    {
        protected IRecorderSettingsHost host;
        protected bool inputFoldout = true;
        protected bool outputFormatFoldout = true;
        protected bool outputFileFoldout = true;
        
        /// <summary>
        /// Draws the complete recorder settings UI
        /// </summary>
        public virtual void DrawRecorderSettings()
        {
            // Input section
            inputFoldout = RecorderUIHelper.DrawHeaderFoldout(inputFoldout, "Input");
            if (inputFoldout)
            {
                RecorderUIHelper.BeginIndentedSection();
                DrawInputSettings();
                RecorderUIHelper.EndIndentedSection();
            }
            
            RecorderUIHelper.DrawSeparator();
            
            // Output Format section
            outputFormatFoldout = RecorderUIHelper.DrawHeaderFoldout(outputFormatFoldout, "Output Format");
            if (outputFormatFoldout)
            {
                RecorderUIHelper.BeginIndentedSection();
                DrawOutputFormatSettings();
                RecorderUIHelper.EndIndentedSection();
            }
            
            RecorderUIHelper.DrawSeparator();
            
            // Output File section
            outputFileFoldout = RecorderUIHelper.DrawHeaderFoldout(outputFileFoldout, "Output File");
            if (outputFileFoldout)
            {
                RecorderUIHelper.BeginIndentedSection();
                DrawOutputFileSettings();
                RecorderUIHelper.EndIndentedSection();
            }
        }
        
        /// <summary>
        /// Draws the input settings specific to this recorder type
        /// </summary>
        protected virtual void DrawInputSettings()
        {
            EditorGUILayout.LabelField("Source", "Game View");
        }
        
        /// <summary>
        /// Draws the output format settings specific to this recorder type
        /// </summary>
        protected abstract void DrawOutputFormatSettings();
        
        /// <summary>
        /// Draws the output file settings
        /// </summary>
        protected abstract void DrawOutputFileSettings();
        
        /// <summary>
        /// Validates the current settings
        /// </summary>
        public virtual bool ValidateSettings(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
    }
}