/**
 * Creates the InputExecutedFlowsRenderer instance and returns it
 * @param dotNetObject The calling dotnet object
 * @param uid The unique identifier of the element
 * @returns the InputExecutedFlowsRenderer instance
 */
export function createInputExecutedFlowsRenderer(dotNetObject, uid)
{
    return new InputExecutedFlowsRenderer(dotNetObject, uid);
}

export class InputExecutedFlowsRenderer {
    constructor(dotnetObject, uid) {
        this.dotnetObject = dotnetObject;
        this.element = document.getElementById(uid);
    }

    /**
     * Checks if the element is visible
     * @returns {boolean} True if the element is visible, false otherwise
     */
    isElementVisible(){
        if (!this.element) return false;
        const rect = this.element.getBoundingClientRect();
        return (
            rect.top < (window.innerHeight || document.documentElement.clientHeight) &&
            rect.bottom > 0 &&
            rect.left < (window.innerWidth || document.documentElement.clientWidth) &&
            rect.right > 0
        );
    }
    
    captureDoubleClicks() {
        this.element.querySelectorAll('.flow-part .draggable').addEventListener("dblclick", (e) => {
            let uid = e.target.parentNode.getAttribute('x-uid');
            this.dotnetObject.invokeMethodAsync('OnDoubleClick', uid);
        });
    }

}