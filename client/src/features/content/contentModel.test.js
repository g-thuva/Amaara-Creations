import { expect, it } from 'vitest';
import { readCmsObject } from './contentModel';
it.each(['{broken', '[]', 'null', 'true', '"text"'])('omits malformed and unsupported CMS data %s', value => expect(readCmsObject(value)).toBeNull());
it('keeps published structured CMS blocks available', () => expect(readCmsObject('{"type":"faq","items":[]}')).toEqual({ type: 'faq', items: [] }));
