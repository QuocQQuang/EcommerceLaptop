// Support CommonJS packages without types
declare module 'react-image-magnify' {
    import type { ComponentType } from 'react';
    const ReactImageMagnify: ComponentType<any>;
    export default ReactImageMagnify;
}

