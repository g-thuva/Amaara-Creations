import { useEffect } from 'react';
export function JsonLd({ id, value }) {
  useEffect(() => {
    const element = document.createElement('script'); element.id = id; element.type = 'application/ld+json'; element.textContent = JSON.stringify(value).replace(/</g, '\\u003c'); document.head.appendChild(element);
    return () => element.remove();
  }, [id, value]);
  return null;
}
