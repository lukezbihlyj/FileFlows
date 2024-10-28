/**
 * Creates the ExecutedFlowElementsRenderer instance and returns it
 * @param dotNetObject The calling dotnet object
 * @param uid The unique identifier of the element
 * @returns the ExecutedFlowElementsRenderer instance
 */
export function createExecutedFlowElementsRenderer(dotNetObject, uid)
{
    return new ExecutedFlowElementsRenderer(dotNetObject, uid);
}

export class ExecutedFlowElementsRenderer {
    constructor(dotnetObject, uid) {
        this.dotnetObject = dotnetObject;
        this.element = document.getElementById(uid);
    }

    /**
     * Checks if the element is visible
     * @returns {boolean} True if the element is visible, false otherwise
     */
    isElementVisible(ele){
        ele = ele || this.element;
        if (!ele) return false;
        const rect = ele.getBoundingClientRect();
        return (
            rect.top < (window.innerHeight || document.documentElement.clientHeight) &&
            rect.bottom > 0 &&
            rect.left < (window.innerWidth || document.documentElement.clientWidth) &&
            rect.right > 0
        );
    }

    getVisibleHeight() {
        // client height
        return window.innerHeight || document.documentElement.clientHeight;
    }

    captureDoubleClicks() {
        for(let part of this.element.querySelectorAll('.flow-part .draggable')) {
            part.addEventListener("dblclick", (e) => {
                let uid = e.target.parentNode.getAttribute('x-uid');
                this.dotnetObject.invokeMethodAsync('OnDoubleClick', uid);
            });
        }
    }
    
    dispose(){
        // remove all child elements
        while (this.element.firstChild) {
            this.element.removeChild(this.element.firstChild);
        }
    }

}