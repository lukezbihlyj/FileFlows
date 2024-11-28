/**
 * Creates the InputBinary instance and returns it
 * @returns {InputCode} the InputBinary instance
 */
export function createInputBinary()
{
    return new InputBinary();
}
/**
 * Class representing an InputBinary for handling file input.
 */
export class InputBinary 
{    
    /**
     * Opens a file dialog and retrieves the selected file's MIME type and content as a Uint8Array.
     * @returns {Promise<{mimeType: string, content: Uint8Array} | null>} An object with the MIME type and file content, or null if no file is selected.
     */
    async chooseFile() {
        return new Promise((resolve) => {
            const input = document.createElement("input");
            input.type = "file";
            input.onchange = async (e) => {
                const file = e.target.files[0];
                if (!file) {
                    resolve(null);
                    return;
                }

                const mimeType = file.type;
                const content = await file.arrayBuffer();
                resolve({mimeType, content: new Uint8Array(content)});
            };
            input.click();
        });
    }
}