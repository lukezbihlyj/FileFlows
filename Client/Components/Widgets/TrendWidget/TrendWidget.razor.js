/**
 * Creates the TrendWidget instance and returns it
 * @param dotNetObject The calling dotnet object
 * @param uid The unique identifier of the widget
 * @returns {InputCode} the TrendWidget instance
 */
export function createTrendWidget(dotNetObject, uid)
{
    return new TrendWidget(dotNetObject, uid);
}

export class TrendWidget {

    constructor(dotnetObject, uid) {
        this.dotnetObject = dotnetObject;
        this.uid = uid;
        this.element = document.getElementById(uid);
        this.head = this.element.querySelector('.head');
        // when the element dimensions change
        this.observer = new ResizeObserver(() => this.onResize());
        this.observer.observe(this.element);
        this.onResize();
    }
    
    onResize() {
        // Clear the previous timeout, if any
        if(this.resizeTimeout)
            clearTimeout(this.resizeTimeout);

        // Set a new timeout to invoke the method after 500ms
        this.resizeTimeout = setTimeout(() => {
            let width = this.element.clientWidth;
            let height = this.element.clientHeight;
            height = height - this.head.clientHeight - 50;
            this.dotnetObject.invokeMethodAsync('OnResize', width, height);
        }, 500); // 500ms delay
    }
    
    dispose()
    {
        this.observer.disconnect();
    }
}