// OLW DOM Bridge - Provides element tracking and DOM operations for WebView2 shim
// Elements are tracked by unique IDs assigned when they cross the JS/C# boundary

(function() {
    'use strict';
    
    const OLW = window.OLW = window.OLW || {};
    
    // Element registry - maps IDs to DOM elements
    const elementRegistry = new Map();
    let nextElementId = 1;
    
    // Get or assign an ID to an element
    function getElementId(element) {
        if (!element) return null;
        let id = element.getAttribute('data-olw-id');
        if (!id) {
            id = 'olw-' + (nextElementId++);
            element.setAttribute('data-olw-id', id);
            elementRegistry.set(id, element);
        }
        return id;
    }
    
    // Get element by ID
    function getElementById(id) {
        if (!id) return null;
        let el = elementRegistry.get(id);
        if (!el) {
            el = document.querySelector(`[data-olw-id="${id}"]`);
            if (el) elementRegistry.set(id, el);
        }
        return el;
    }
    
    // Wrap an element for return to C# (returns ID)
    function wrapElement(element) {
        if (!element) return null;
        return getElementId(element);
    }
    
    // Wrap multiple elements
    function wrapElements(elements) {
        if (!elements) return [];
        const result = [];
        for (let i = 0; i < elements.length; i++) {
            result.push(wrapElement(elements[i]));
        }
        return result;
    }
    
    // ===== Element Operations =====
    
    OLW.element = {
        // Get properties
        getInnerHTML: (id) => getElementById(id)?.innerHTML,
        getInnerText: (id) => getElementById(id)?.innerText,
        getOuterHTML: (id) => getElementById(id)?.outerHTML,
        getOuterText: (id) => getElementById(id)?.outerText,
        getClassName: (id) => getElementById(id)?.className,
        getId: (id) => getElementById(id)?.id,
        getTagName: (id) => getElementById(id)?.tagName,
        getTitle: (id) => getElementById(id)?.title,
        getLang: (id) => getElementById(id)?.lang,
        
        // Set properties
        setInnerHTML: (id, value) => { const el = getElementById(id); if (el) el.innerHTML = value; },
        setInnerText: (id, value) => { const el = getElementById(id); if (el) el.innerText = value; },
        setOuterHTML: (id, value) => { const el = getElementById(id); if (el) el.outerHTML = value; },
        setOuterText: (id, value) => { const el = getElementById(id); if (el) el.outerText = value; },
        setClassName: (id, value) => { const el = getElementById(id); if (el) el.className = value; },
        setId: (id, value) => { const el = getElementById(id); if (el) el.id = value; },
        setTitle: (id, value) => { const el = getElementById(id); if (el) el.title = value; },
        setLang: (id, value) => { const el = getElementById(id); if (el) el.lang = value; },
        
        // Offset/size properties
        getOffsetLeft: (id) => getElementById(id)?.offsetLeft ?? 0,
        getOffsetTop: (id) => getElementById(id)?.offsetTop ?? 0,
        getOffsetWidth: (id) => getElementById(id)?.offsetWidth ?? 0,
        getOffsetHeight: (id) => getElementById(id)?.offsetHeight ?? 0,
        getOffsetParent: (id) => wrapElement(getElementById(id)?.offsetParent),
        getSourceIndex: (id) => {
            const el = getElementById(id);
            if (!el) return -1;
            const all = document.getElementsByTagName('*');
            for (let i = 0; i < all.length; i++) {
                if (all[i] === el) return i;
            }
            return -1;
        },
        
        // Navigation
        getParentElement: (id) => wrapElement(getElementById(id)?.parentElement),
        getChildren: (id) => wrapElements(getElementById(id)?.children),
        getAllDescendants: (id) => wrapElements(getElementById(id)?.getElementsByTagName('*')),
        
        // Attributes
        getAttribute: (id, name) => getElementById(id)?.getAttribute(name),
        setAttribute: (id, name, value) => getElementById(id)?.setAttribute(name, value),
        removeAttribute: (id, name) => getElementById(id)?.removeAttribute(name) ?? false,
        
        // Methods
        click: (id) => getElementById(id)?.click(),
        scrollIntoView: (id, alignTop) => getElementById(id)?.scrollIntoView(alignTop !== false),
        contains: (id, childId) => getElementById(id)?.contains(getElementById(childId)) ?? false,
        insertAdjacentHTML: (id, position, html) => getElementById(id)?.insertAdjacentHTML(position, html),
        insertAdjacentText: (id, position, text) => getElementById(id)?.insertAdjacentText(position, text),
        
        // Style
        getStyleProperty: (id, prop) => getElementById(id)?.style[prop],
        setStyleProperty: (id, prop, value) => { 
            const el = getElementById(id); 
            if (el) el.style[prop] = value; 
        },
        getComputedStyle: (id, prop) => {
            const el = getElementById(id);
            if (!el) return null;
            return window.getComputedStyle(el)[prop];
        },
        
        // Check if element is text editable
        isTextEdit: (id) => {
            const el = getElementById(id);
            if (!el) return false;
            return el.isContentEditable || el.tagName === 'INPUT' || el.tagName === 'TEXTAREA';
        }
    };
    
    // ===== Document Operations =====
    
    OLW.document = {
        getBody: () => wrapElement(document.body),
        getDocumentElement: () => wrapElement(document.documentElement),
        getActiveElement: () => wrapElement(document.activeElement),
        
        getElementById: (id) => wrapElement(document.getElementById(id)),
        getElementsByTagName: (tagName) => wrapElements(document.getElementsByTagName(tagName)),
        getElementsByClassName: (className) => wrapElements(document.getElementsByClassName(className)),
        querySelector: (selector) => wrapElement(document.querySelector(selector)),
        querySelectorAll: (selector) => wrapElements(document.querySelectorAll(selector)),
        
        createElement: (tagName) => wrapElement(document.createElement(tagName)),
        createTextNode: (text) => {
            // Text nodes need special handling - wrap in a span for tracking
            const span = document.createElement('span');
            span.appendChild(document.createTextNode(text));
            return wrapElement(span);
        },
        
        getReadyState: () => document.readyState,
        getTitle: () => document.title,
        setTitle: (value) => { document.title = value; },
        getURL: () => document.URL,
        
        elementFromPoint: (x, y) => wrapElement(document.elementFromPoint(x, y)),
        
        // execCommand for formatting
        execCommand: (cmd, showUI, value) => document.execCommand(cmd, showUI, value),
        queryCommandState: (cmd) => document.queryCommandState(cmd),
        queryCommandValue: (cmd) => document.queryCommandValue(cmd),
        queryCommandEnabled: (cmd) => document.queryCommandEnabled(cmd),
        
        // HTML content
        write: (html) => document.write(html),
        writeln: (html) => document.writeln(html)
    };
    
    // ===== Selection Operations =====
    
    OLW.selection = {
        getType: () => {
            const sel = window.getSelection();
            if (!sel || sel.rangeCount === 0) return 'None';
            if (sel.isCollapsed) return 'Caret';
            return 'Text';
        },
        
        getText: () => window.getSelection()?.toString() ?? '',
        
        getHtml: () => {
            const sel = window.getSelection();
            if (!sel || sel.rangeCount === 0) return '';
            const range = sel.getRangeAt(0);
            const div = document.createElement('div');
            div.appendChild(range.cloneContents());
            return div.innerHTML;
        },
        
        getAnchorElement: () => {
            const sel = window.getSelection();
            if (!sel || !sel.anchorNode) return null;
            const node = sel.anchorNode.nodeType === 1 ? sel.anchorNode : sel.anchorNode.parentElement;
            return wrapElement(node);
        },
        
        getFocusElement: () => {
            const sel = window.getSelection();
            if (!sel || !sel.focusNode) return null;
            const node = sel.focusNode.nodeType === 1 ? sel.focusNode : sel.focusNode.parentElement;
            return wrapElement(node);
        },
        
        collapse: (toStart) => {
            const sel = window.getSelection();
            if (sel && sel.rangeCount > 0) {
                if (toStart) sel.collapseToStart();
                else sel.collapseToEnd();
            }
        },
        
        selectAll: () => document.execCommand('selectAll'),
        
        clear: () => window.getSelection()?.removeAllRanges(),
        
        // Select an element's contents
        selectElement: (id) => {
            const el = getElementById(id);
            if (!el) return;
            const range = document.createRange();
            range.selectNodeContents(el);
            const sel = window.getSelection();
            sel.removeAllRanges();
            sel.addRange(range);
        }
    };
    
    // ===== TextRange-like Operations =====
    // Simulates IHTMLTxtRange functionality using modern Range API
    
    const textRanges = new Map();
    let nextRangeId = 1;
    
    OLW.textRange = {
        create: () => {
            const range = document.createRange();
            const id = 'range-' + (nextRangeId++);
            textRanges.set(id, range);
            return id;
        },
        
        createFromSelection: () => {
            const sel = window.getSelection();
            if (!sel || sel.rangeCount === 0) return null;
            const range = sel.getRangeAt(0).cloneRange();
            const id = 'range-' + (nextRangeId++);
            textRanges.set(id, range);
            return id;
        },
        
        dispose: (id) => textRanges.delete(id),
        
        getText: (id) => textRanges.get(id)?.toString() ?? '',
        setText: (id, text) => {
            const range = textRanges.get(id);
            if (range) {
                range.deleteContents();
                range.insertNode(document.createTextNode(text));
            }
        },
        
        getHtml: (id) => {
            const range = textRanges.get(id);
            if (!range) return '';
            const div = document.createElement('div');
            div.appendChild(range.cloneContents());
            return div.innerHTML;
        },
        
        setHtml: (id, html) => {
            const range = textRanges.get(id);
            if (!range) return;
            range.deleteContents();
            const temp = document.createElement('div');
            temp.innerHTML = html;
            const frag = document.createDocumentFragment();
            while (temp.firstChild) {
                frag.appendChild(temp.firstChild);
            }
            range.insertNode(frag);
        },
        
        select: (id) => {
            const range = textRanges.get(id);
            if (!range) return;
            const sel = window.getSelection();
            sel.removeAllRanges();
            sel.addRange(range);
        },
        
        collapse: (id, toStart) => {
            const range = textRanges.get(id);
            if (range) range.collapse(toStart !== false);
        },
        
        moveToElement: (id, elementId) => {
            const range = textRanges.get(id);
            const el = getElementById(elementId);
            if (range && el) range.selectNodeContents(el);
        },
        
        getParentElement: (id) => {
            const range = textRanges.get(id);
            if (!range) return null;
            return wrapElement(range.commonAncestorContainer.nodeType === 1 
                ? range.commonAncestorContainer 
                : range.commonAncestorContainer.parentElement);
        },
        
        execCommand: (id, cmd, value) => {
            const range = textRanges.get(id);
            if (!range) return false;
            // Select the range, execute command, then restore
            const sel = window.getSelection();
            const oldRanges = [];
            for (let i = 0; i < sel.rangeCount; i++) {
                oldRanges.push(sel.getRangeAt(i).cloneRange());
            }
            sel.removeAllRanges();
            sel.addRange(range);
            const result = document.execCommand(cmd, false, value);
            sel.removeAllRanges();
            oldRanges.forEach(r => sel.addRange(r));
            return result;
        }
    };
    
    // ===== Utility =====
    
    OLW.util = {
        // Clean up orphaned element references
        gc: () => {
            const toDelete = [];
            elementRegistry.forEach((el, id) => {
                if (!document.body.contains(el)) {
                    toDelete.push(id);
                }
            });
            toDelete.forEach(id => elementRegistry.delete(id));
            return toDelete.length;
        },
        
        // Get element by OLW ID (for debugging)
        getById: (id) => getElementById(id)
    };
    
    console.log('[OLW] DOM Bridge loaded');
})();
