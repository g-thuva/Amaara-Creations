import { useCart } from '@/features/cart/CartProvider.jsx';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { useResource } from '@/shared/hooks/useResource.js';
import { customDesignApi } from '@/features/custom-design/customDesignApi.js';

export function useSavedDesigns() {
  const designs = useResource(() => customDesignApi.listDesigns({ pageSize: 50 }), 'saved-designs');
  const { cart } = useCart();
  const { mutate, pending } = useAuthenticatedMutation();
  const act = (action, message) => mutate(action, designs.reload, message);
  const add = (id) => mutate(async () => { await customDesignApi.addToCart(id); await cart.reload(); }, designs.reload, 'Custom design added to your cart.');

  const duplicate = id => act(() => customDesignApi.duplicateDesign(id), 'Design duplicated.');
  const archive = id => act(() => customDesignApi.archiveDesign(id), 'Design archived.');
  return { designs, pending, duplicate, archive, add };
}
